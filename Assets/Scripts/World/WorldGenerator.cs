using System;
using System.Collections.Generic;
using UnityEngine;
using Wozware.Poolers;

namespace Wozware.Downslope
{
	public sealed partial class WorldGenerator : MonoBehaviour
	{
		#region Events

		// events
		public event Action OnUpdated;
		public event Action OnClearedWorld;
		public event Action<float> OnChangedWorldSpeed;
		public event Action<float> OnUpdatedKMH;
		public event Action<float> OnUpdatedDistanceTravelled;

		// actions
		public Func<string, bool> CreateSFX;
		public Action<float> ShiftWorldCameraX;
		public Func<float> GetPlayerXPosition;

		#endregion

		#region Public Members

		public AssetPack Assets;
		public SpriteAssetPack Sprites;
		public WozPooler<WorldSprite> PrimarySpritePooler;
		public WozPooler<WorldChunk> PrimaryChunkPooler;
		public Transform WorldCenter;
		public Transform ActiveChunkParent;
		public Transform ActiveObjectParent;
		public Transform ActiveFXParent;
		public SortingLayer ObstacleSortingLayer;
		public LayerMask ImpassableLayer;

		public int ObstacleSeed = 10403;
		public int PathForwardGenerationDistance;
		public int WorldEdgeSize = 14;
		public int MinWorldEdgeSize = 4;
		public int MaxWorldEdgeSize = 10;
		public int DefaultObstacleLayer = 3;
		public int DefaultWorldXOffset = 0;
		public int MaxWorldXOffset = 12;
		public float XOffsetShiftHeight = 0;
		public float XOffsetShiftDiff = 0;
		public float XOffsetPlayerDiff = 0;
		public float LOSUpdateRate = 0.02f;
		public float WorldSpeed;
		public float WorldDestroyY;
		public float WorldGenYHeight;
		public float MetricSpeedScale = 3f;

		public readonly Vector3 WorldDirection = Vector3.up;

		#endregion

		#region Private Members

		private bool _firstPath = true;
		private int _lastIcePathID;
		private int _lastChunkID;
		private float _currMaxChunkWidth = 0;
		private float _leftWorldEdge = 0;
		private float _rightWorldEdge = 0;
		private float _currLeftBorder = 0;
		private float _currRightBorder = 0;
		private bool _worldShifted = false;
		[SerializeField] private float _currWorldXOffset = 0;
		private string _currWidthIcePathName = "Path1Flat";
		private readonly int _chunkDirection = -1;

		[ReadOnly][SerializeField] private float _distanceTravelled = 0f;
		[ReadOnly][SerializeField] private float _metersPerSecond = 0f;
		[ReadOnly][SerializeField] private float _kmh = 0f;

		// active lists
		private Dictionary<int, WorldObject> _activeObjects;
		private Dictionary<int, WorldSprite> _activeSprites;
		private Dictionary<int, WorldChunk> _activeChunks;

		private float _lastDistance = 0f;

		#endregion

		#region Unity Methods

		private void Awake()
		{
			InitializeStructures();
			_currWorldXOffset = DefaultWorldXOffset;
		}

		private void Start()
		{
			InitializeEvents();
		}

		private void Update()
		{
			OnUpdated();
		}

		private void FixedUpdate()
		{
			// OnUpdateWorldMovement.Invoke(WorldSpeed * DownslopeTime.TimeScale);
			UpdateDistanceTravelled(WorldSpeed * DownslopeTime.TimeScale);
		}

		#endregion

		#region Public Methods

		/// <returns> The current speed in KMH of the world. </returns>
		public float KMH()
		{
			return _kmh;
		}

		/// <returns> The current distance travelled in the world. </returns>
		public int DistanceTravelled()
		{
			return (int)_distanceTravelled;
		}

		/// <summary> Sets the root WorldIcePath type to be spawned.  </summary>
		public void SetFirstIcePathName(string id)
		{
			_currWidthIcePathName = id;
		}

		/// <summary> Sets the current world edge size of a new generated WorldChunk. </summary>
		public void SetWorldEdgeSize(int size, int minSize)
		{
			WorldEdgeSize = size;
			_leftWorldEdge = WorldEdgeSize;
			_rightWorldEdge = _leftWorldEdge;
			MaxWorldEdgeSize = size;
			MinWorldEdgeSize = minSize;
		}

		/// <summary> Sets the current max width of a new generated WorldChunk. </summary>
		public void SetMaxChunkWidth(int width)
		{
			_currMaxChunkWidth = width;
		}

		/// <summary> Clears the currently generated world. </summary>
		public void ClearWorld()
		{
			Util.Log("Clear World", this.name);
			// all chunks subscribed to this event will be destroyed
			OnClearedWorld();

			_lastIcePathID = 0;
			_lastChunkID = 0;
			_lastDistance = 0;
			_distanceTravelled = 0;
			_activeObjects.Clear();
			_activeSprites.Clear();
			_firstPath = true;
		}

		public void SetWorldSpeed(float speed)
		{
			WorldSpeed = speed;
			OnChangedWorldSpeed(WorldSpeed * DownslopeTime.TimeScale);
		}

		public void ArcadeWorldGenerateNext()
		{
			ArcadeWorldGenerateNextChunks();
		}

		public void CheckAIReachedGenThreshold(float yPosition, float threshold)
		{
			WorldChunk chunk = _activeChunks[_lastChunkID];
			if (yPosition - chunk.transform.position.y < threshold)
			{
				WorldAutoGenerate(chunk);
			}
		}

		#endregion

		#region Private Methods

		/// <summary> Initializes essential event callbacks. </summary>
		private void InitializeEvents()
		{
			OnUpdated = () => { };
			OnChangedWorldSpeed = (speed) => { };
			OnUpdatedDistanceTravelled = (f) => { };
			OnUpdatedKMH = (f) => { };
			OnClearedWorld = () => { };

			// OnSetWorldSpeed += UpdateDistanceTravelled;

			PrimarySpritePooler.OnAddToPool += ActivatePooledSprite;
			PrimarySpritePooler.OnReturnToPool += DeactivatePooledSprite;
			PrimarySpritePooler.OnDestroyExcess += DestroyPooledSprite;

			OnUpdated += PrimarySpritePooler.CheckTrim;

			PrimaryChunkPooler.OnAddToPool += ActivatePooledChunk;
			PrimaryChunkPooler.OnReturnToPool += DeactivatePooledChunk;
			PrimaryChunkPooler.OnDestroyExcess += DestroyPooledChunk;

			OnUpdated += PrimaryChunkPooler.CheckTrim;
		}

		/// <summary> Initializes essential structures for world generation. </summary>
		private void InitializeStructures()
		{
			_activeObjects = new();
			_activeSprites = new();
			_activeChunks = new();

			_currWeightedLayers = new();
			_currActiveChunkFlags = new();
			_currActiveIcePaths = new();
			_currActiveIcePathObjects = new();
			_currOccupiedIce = new();
			_lastCreatedIcePaths = new();

			PrimarySpritePooler.Initialize();
			PrimaryChunkPooler.Initialize();
		}

		/// <summary> Initializes a new WorldSprite. </summary>
		private void InitializeSpriteObject(WorldSprite worldSprite, SpriteAnimation sprite, SpriteLayerSortData sortData)
		{
			worldSprite.SetSortingLayer(sortData.LayerName);
			worldSprite.SetSortingOrder(sortData.LayerID);
			worldSprite.SetSpriteAnimation(sprite);
			worldSprite.SetSprite(sprite.DefaultAnimation[0].Sprite);
		}

		/// <summary> Initializes a new Unity GameObject. </summary>
		private void InitializeGameObject(GameObject obj, Vector3 pos, Transform parent)
		{
			obj.transform.position = pos;
			obj.transform.SetParent(parent);
			obj.SetActive(true);
		}

		/// <summary> Initializes a WorldSprite given a new GameObject with a WorldSprite. </summary>
		private void InitializeWorldSprite(int uid, GameObject gameObj, WorldSprite sprite, Action<WorldObject> worldDestroyCallback)
		{
			// initialize props
			WorldObjectProps props = new WorldObjectProps(uid, gameObj);
			WorldMovementProps movement = new WorldMovementProps(WorldDirection, WorldDestroyY, WorldGenYHeight);

			// add to structures
			_activeObjects.Add(uid, sprite);
			_activeSprites.Add(uid, sprite);

			sprite.Initialize(props, movement);
			sprite.SetObjectSpeed(WorldSpeed * DownslopeTime.TimeScale);

			// subscribe to events
			OnChangedWorldSpeed += _activeObjects[uid].SetObjectSpeed;
			OnClearedWorld += _activeObjects[uid].TriggerDestroy;
			_activeObjects[uid].OnDestroyTriggered += worldDestroyCallback;
		}

		/// <summary> Initializes a WorldSprite part of a WorldChunk given a new GameObject with a WorldSprite. </summary>
		private void InitializeChunkedWorldSprite(int uid, GameObject gameObj, WorldSprite sprite, Action<WorldObject> destroyCallback)
		{
			// initialize props
			WorldObjectProps props = new WorldObjectProps(uid, gameObj);
			sprite.HasParentChunk = true;
			sprite.IsParentChunk = false;
			sprite.ParentChunk = _activeChunks[_lastChunkID];

			// add to structures
			_activeObjects.Add(uid, sprite);
			_activeSprites.Add(uid, sprite);

			_activeObjects[uid].OnDestroyTriggered += destroyCallback;

			sprite.Initialize(props);
		}

		/// <summary> Initializes a new WorldChunk object. </summary>
		private void InitializeChunkObject(WorldChunk chunk, Action<WorldObject> destroyCallback)
		{
			int uid = chunk.GetHashCode();
			WorldMovementProps movement = new WorldMovementProps(WorldDirection, WorldDestroyY, WorldGenYHeight);
			WorldObjectProps props = new WorldObjectProps(uid, chunk.gameObject);
			chunk.Initialize(props, movement);
			chunk.CountID = _currChunkCountID;
			chunk.UID = uid;
			chunk.IsParentChunk = true;
			chunk.SetObjectSpeed(WorldSpeed * DownslopeTime.TimeScale);
			chunk.OffsetCorrectionHeight = XOffsetShiftHeight;
			chunk.XOffset = _currWorldXOffset;
			chunk.OnSelfReachedOffsetThreshold += ChunkWithOffsetReachThreshold;
			chunk.gameObject.SetActive(true);

			_activeChunks.Add(uid, chunk);
			OnChangedWorldSpeed += _activeChunks[uid].SetObjectSpeed;
			_activeChunks[uid].OnDestroyTriggered += destroyCallback;
			OnClearedWorld += _activeChunks[uid].TriggerDestroy;

			// cache this uid as the last uid created
			_lastChunkID = uid;
		}

		/// <summary> Initializes a newly created ice path object. </summary>
		private void InitializeIcePathObject(WorldIcePath path, Action<WorldIcePath> destroyCallback)
		{
			// initialize props
			int uid = path.GetHashCode();
			WorldMovementProps movement = new WorldMovementProps(WorldDirection, WorldDestroyY, WorldGenYHeight);
			WorldObjectProps props = new WorldObjectProps(uid, path.gameObject);
			path.WorldObj.CountID = _currIcePathCountID;
			path.WorldObj.UID = uid;
			path.Initialize(props, movement);
			path.SetMovement(movement);
			path.WorldObj.SetObjectSpeed(WorldSpeed * DownslopeTime.TimeScale);

			// add to structures
			_currActiveIcePathObjects.Add(uid, path);

			// subscribe to events
			OnChangedWorldSpeed += _currActiveIcePathObjects[uid].WorldObj.SetObjectSpeed;
			_currActiveIcePathObjects[uid].OnDestroy += destroyCallback;
			OnClearedWorld += _currActiveIcePathObjects[uid].TriggerDestroy;

			// cache this uid as the last uid created
			_lastIcePathID = uid;
		}

		/// <summary> Callback to a objects reached height event. Generates the next forward chunk. </summary>
		private void WorldAutoGenerate(WorldObject obj)
		{
			obj.ReachHeightThresholdUnsubscribe(WorldAutoGenerate);
			ArcadeWorldGenerateNextChunks();
		}

		private void ActivatePooledSprite(WorldSprite sprite)
		{
			sprite.UID = sprite.GetHashCode();
			sprite.gameObject.SetActive(false);
			sprite.name = $"PooledSprite[{sprite.UID}]";
		}

		private void DeactivatePooledSprite(WorldSprite sprite)
		{
			sprite.SetParent(PrimarySpritePooler.PoolParent);
			sprite.gameObject.SetActive(false);
			sprite.name = $"PooledSprite[{sprite.UID}]";
			sprite.ResetSprite();
		}

		private void DestroyPooledSprite(WorldSprite sprite)
		{
			Destroy(sprite);
		}

		private void ActivatePooledChunk(WorldObject chunk)
		{
			chunk.UID = chunk.GetHashCode();
			chunk.gameObject.SetActive(false);
			chunk.name = $"PooledChunk[{chunk.UID}]";
		}

		private void DeactivatePooledChunk(WorldObject chunk)
		{
			chunk.SetParent(PrimaryChunkPooler.PoolParent);
			chunk.gameObject.SetActive(false);
			chunk.name = $"PooledChunk[{chunk.UID}]";
		}

		private void DestroyPooledChunk(WorldObject chunk)
		{
			Destroy(chunk);
		}

		/// <summary> Callback for when a chunk should be destroyed. </summary>
		private void DestroyChunk(WorldObject chunk)
		{
			int uid = chunk.UID;
			uint id = chunk.CountID;

			// unsub events
			OnChangedWorldSpeed -= _activeChunks[uid].SetObjectSpeed;
			_activeChunks[uid].OnDestroyTriggered -= DestroyChunk;
			OnClearedWorld -= _activeChunks[uid].TriggerDestroy;
			chunk.OnSelfReachedOffsetThreshold -= ChunkWithOffsetReachThreshold;

			// return to pool
			PrimaryChunkPooler.ReturnToPool(_activeChunks[uid]);
			_activeChunks.Remove(uid);
		}

		/// <summary> Callback for when a chunk should be destroyed. </summary>
		private void DestroyIcePath(WorldIcePath path)
		{
			// unsub events
			int uid = path.WorldObj.UID;
			uint id = path.WorldObj.CountID;
			OnChangedWorldSpeed -= _currActiveIcePathObjects[uid].WorldObj.SetObjectSpeed;
			_currActiveIcePathObjects[uid].OnDestroy -= DestroyIcePath;
			OnClearedWorld -= _currActiveIcePathObjects[uid].TriggerDestroy;
			_currActiveIcePathObjects.Remove(uid);


			// destroy path
			Destroy(path.gameObject);
		}

		/// <summary> Destroy WorldSprite object given id. </summary>
		private void DestroyWorldSpriteObject(int id)
		{
			// unsub events
			OnChangedWorldSpeed -= _activeObjects[id].SetObjectSpeed;

			// return to pool
			PrimarySpritePooler.ReturnToPool(_activeSprites[id]);

			// remove object
			_activeSprites.Remove(id);
			_activeObjects.Remove(id);
		}

		/// <summary> Destroy obstacle given WorldObject. </summary>
		/// <param name="obj"> The base tile to destroy. </param>
		private void DestroyObstacleObject(WorldObject obj)
		{
			int id = obj.UID;

			// unsub events
			_activeObjects[id].OnDestroyTriggered -= DestroyObstacleObject;
			// _activeSprites[id].OnObjectCollide -= DestroyObstacleObject;
			OnClearedWorld -= _activeObjects[id].TriggerDestroy;
			_activeSprites[id].PlaySFX = null;
			_activeSprites[id].CreatePFX = null;

			// set to inactive default layer
			_activeSprites[id].gameObject.layer = LayerMask.NameToLayer("Default");

			// call base
			DestroyWorldSpriteObject(id);
		}

		/// <summary> Destroy obstacle that is part of a chunk given a WorldObject. </summary>
		/// <param name="obj"> The base world object to destroy. </param>
		private void DestroyChunkedObstacleObject(WorldObject obj)
		{
			// unsub self destroy event
			//obj.ParentChunk.OnDestroyTriggered -= DestroyChunkedObstacleObject;
			_activeObjects[obj.UID].OnDestroyTriggered -= DestroyChunkedObstacleObject;

			DestroyChunkedObstacleObjectById(obj.UID);
		}

		/// <summary> Destroy obstacle that is part of a chunk given a WorldObject. </summary>
		/// <param name="obj"> The uid of the object. </param>
		private void DestroyChunkedObstacleObjectById(int id)
		{
			// unsub collide event
			// _activeSprites[id].OnObjectCollide -= DestroyChunkedObstacleObjectById;

			_activeSprites[id].PlaySFX = null;
			_activeSprites[id].CreatePFX = null;

			// set to inactive default layer
			_activeSprites[id].gameObject.layer = LayerMask.NameToLayer("Default");

			// call base
			DestroyWorldSpriteObject(id);
		}

		/// <summary> Destroy obstacle given WorldObject id. </summary>
		/// <param name="uid"> The UID of the object to destroy. </param>
		private void DestroyObstacleObject(int id)
		{
			if (!_activeObjects.ContainsKey(id))
				return;

			// unsub events
			// _activeSprites[id].OnObjectCollide -= DestroyObstacleObject;
			_activeObjects[id].OnDestroyTriggered -= DestroyObstacleObject;

			// call base
			DestroyWorldSpriteObject(id);
		}

		/// <summary> Updates the distance travelled and other metrics given speed </summary>
		/// <param name="speed"> The speed to update by. </param>
		private void UpdateDistanceTravelled(float speed)
		{
			// update distance travelled
			_distanceTravelled += speed;
			OnUpdatedDistanceTravelled(_distanceTravelled);

			// distance traveled excludes horizontal movement

			// forward mps is the difference between distance travelled and last distance
			// multiplied by 50 (fixed update is 0.02s, which is 50 per sec)
			_metersPerSecond = (_distanceTravelled - _lastDistance) * 50;

			// 1 mps = 3.6 kmh
			_kmh = _metersPerSecond * 3.6f;
			OnUpdatedKMH(_kmh);

			// last distance is now the distance travelled
			_lastDistance = _distanceTravelled;
		}

		private void ChunkWithOffsetReachThreshold(WorldObject chunk, float xOffset)
		{
			// unsubscribe events from chunk first
			chunk.OnSelfReachedOffsetThreshold -= ChunkWithOffsetReachThreshold;

			Debug.Log("hi2");

			if (ShiftWorldCameraX != null)
			{
				float leftEdge = _currWorldXOffset - _currMaxChunkWidth;
				float rightEdge = _currWorldXOffset + _currMaxChunkWidth;
				float playerX = GetPlayerXPosition();
				int center = (int)playerX + 1;

				bool playerDiffLeft = Mathf.Abs(leftEdge - playerX) < XOffsetPlayerDiff;
				bool playerDiffRight = Mathf.Abs(rightEdge - playerX) < XOffsetPlayerDiff;
				bool playerDiffCenter = center - playerX <= 1;

				Debug.Log("hi");

				//bool offsetDiff = Mathf.Abs(_currWorldXOffset - xOffset) > XOffsetShiftDiff;
				if (playerDiffLeft || playerDiffRight || playerDiffCenter)
				{
					ShiftWorldCameraX((int)playerX);
				}
			}
		}

		#endregion

	}
}


