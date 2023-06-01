using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.U2D;
using Wozware.Poolers;
using UnityEngine.Experimental.Rendering;
using System.Drawing;
using UnityEngine.Rendering;

namespace Wozware.Downslope
{
	public sealed class WorldGenerator : MonoBehaviour
	{
		public WorldFrameUpdating OnUpdate;
		public WorldMovementUpdating OnUpdateWorldMovement;
		public WorldSpritePooler SpritePooler;
		public DistanceUpdating OnUpdateDistanceTravelled;
		public SpeedUpdating OnUpdateKMH;

		public AssetPack Assets;
		public WozPooler<WorldSprite> PrimarySpritePooler;
		public Transform WorldCenter;
		public Transform ActiveChunkParent;
		public Transform ActiveObjectParent;
		public Transform ActiveFXParent;
		public SortingLayer ObstacleSortingLayer;

		public int ObstacleSeed = 10403;
		public int PathForwardGenerationDistance;
		public int WorldEdgeStart = 14;
		public int WorldEdgeEnd = 20;
		public int DefaultObstacleLayer = 3;
		public float WorldSpeed;
		public float WorldDestroyY;
		public float WorldGenYHeight;
		public float MetricSpeedScale = 3f;

		private bool _firstPath = true;
		private int _lastChunkID;
		private string _currWidthChunkName = "Path1Flat";
		[SerializeField] private float _distanceTravelled = 0f;
		[SerializeField] private float _metersPerSecond = 0f;
		[SerializeField] private float _kmh = 0f;

		private System.Random _obstacleRnd;
		private System.Random _outerObstacleRnd;
		private System.Random _icePathRnd;
		private DDRandom _distributionObstacle;
		private DDRandom _distributionOuterObstacle;
		private DDRandom _distributionIcePath;

		private readonly Vector3 _worldDirection = Vector3.up;
		private readonly int _chunkDirection = -1;
		private List<int> _spriteIds = new List<int>();
		private List<int> _chunkIds = new List<int>();
		private HashSet<float> _currentChunkObstacles;

		private Dictionary<int, WorldObject> _activeObjects;
		private Dictionary<int, WorldSprite> _activeSprites;
		private Dictionary<int, WorldChunk> _activeChunks;

		private float _lastDistance = 0f;

		#region Public Methods

		public float KMH()
		{
			return _kmh;
		}

		/// <summary> Generates the next random (chunks, obstacles, etc) 
		/// forward world from last generated position. </summary>
		public void GenerateForward()
		{
			Vector3 defaultVector = Vector3.zero;
			Vector3 pos = defaultVector + WorldCenter.position;
			Vector3 lastChunkPos = pos;
			int chunkId = 1;

			if (!_firstPath)
			{
				// if not first path, last pos is the last active chunk y - 1
				lastChunkPos.y = _activeChunks[_lastChunkID].transform.position.y;

				// get a new random directional path width from last chunks possibilities
				//RandomizeIcePath();
			}

			// randomize new height of generation
			int chunkAbove = 0;

			for (int i = 0; i < PathForwardGenerationDistance; i++)
			{
				// pos of chunk is current height + the last chunk y
				pos = new Vector3(0, _chunkDirection, 0) + lastChunkPos;

				chunkAbove = _lastChunkID;

				// create the appropriate width chunk
				CreateChunk(pos, _currWidthChunkName);
				RandomizeIcePath();
				_currentChunkObstacles.Clear();

				// generate row obstacle
				GenerateRowObstacle(_lastChunkID);
				GenerateRowOuterObstacle(_activeChunks[_lastChunkID].transform.position.y);

				// generate the final level edges
				GenerateRowLevelEdge(_activeChunks[_lastChunkID].transform.position.y);
				if(chunkAbove == _lastChunkID)
				{
					Debug.Log($"Stuck On Chunk {_lastChunkID}");
				}

				lastChunkPos = pos;
				if(i == 0)
				{
					// chunk finished, subscribe to its height event
					// chunk will auto generate next chunk when its passed the world height threshold
					_activeChunks[_lastChunkID].ReachHeightThresholdSubscribe(WorldAutoGenerate);
				}

				//Debug.Log($"Creating Obstacles For Chunk: {_lastChunkID}");
			}

			_firstPath = false;
		}

		/// <summary> Clears the currently generated world. </summary>
		public void ClearWorld()
		{
			// destroy all chunks
			for (int i = 0; i < _chunkIds.Count; i++)
			{
				DestroyChunk(_activeChunks[_chunkIds[i]]);
			}

			// destroys all obstacles
			for (int i = 0; i < _spriteIds.Count; i++)
			{
				DestroyObstacleObject(_activeObjects[_spriteIds[i]]);
			}

			_lastChunkID = 0;
			_spriteIds.Clear();
			_chunkIds.Clear();
			_activeObjects.Clear();
			_activeSprites.Clear();
		}

		public void UpdateWorldSpeed(float speed)
		{
			WorldSpeed = speed;
		}

		public void CreatePlayerTrail(Vector3 pos, int id)
		{
			// create trail props
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Assets.PlayerTrailSprites[id], 1, ActiveObjectParent);

			// try create sprite
			WorldSprite sprite;
			bool spriteSuccess = CreateWorldObject(objProps, DestroyObstacleObject, out sprite);
			if(!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerTrail failed. CreateWorldObject call returned false.");
				return;
			}

			sprite.name = $"TrailFX[{Assets.PlayerTrailSprites[id].name}][{sprite.UID}]";
		}

		public void CreatePlayerPowderFX(Vector3 pos)
		{
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Assets.GetSprite(SpriteID.EMPTY), 0, ActiveFXParent);
			WorldSprite fxSprite;
			bool spriteSuccess = CreateWorldObject(objProps, DestroyObstacleObject, out fxSprite);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerPowderFX failed. CreateWorldObject call returned false.");
				return;
			}
			GameObject g = Instantiate(Assets.CollidePlayerFX[CollisionTypes.Powder], fxSprite.transform.position, Quaternion.identity, fxSprite.transform);
			fxSprite.name = $"FX[{Assets.CollidePlayerFX[CollisionTypes.Powder].name}][{fxSprite.UID}]";
			Destroy(g, 1f);
		}

		public void CreatePFXSprite(string id, Vector3 pos)
		{
			WorldSpriteProps objProps = new WorldSpriteProps(pos, Assets.GetSprite(SpriteID.EMPTY), 0, ActiveFXParent);
			WorldSprite fxSprite;
			bool spriteSuccess = CreateWorldObject(objProps, DestroyObstacleObject, out fxSprite);
			if (!spriteSuccess)
			{
				Debug.LogError($"World CreatePlayerPowderFX failed. CreateWorldObject call returned false.");
				return;
			}

			ParticleSystem pfx;
			if(!Assets.TryCreatePFX(id, pos, out pfx, fxSprite.transform))
			{
				return;
			}

			Destroy(pfx.gameObject, pfx.main.startLifetime.constant + 0.1f);
			fxSprite.name = $"FX[{pfx.name}][{fxSprite.UID}]";
		}

		#endregion

		#region Private Methods

		/// <summary> Initializes event callbacks and lambdas. </summary>
		private void InitializeEvents()
		{
			OnUpdate = () => { };
			OnUpdateWorldMovement = (speed) => { };
			OnUpdateDistanceTravelled = (f) => { };
			OnUpdateKMH = (f) => { };

			OnUpdateWorldMovement += UpdateDistanceTravelled;

			PrimarySpritePooler.OnAddToPool += WorldSpritePoolAdded;
			PrimarySpritePooler.OnReturnToPool += WorldSpritePoolReturned;
			PrimarySpritePooler.OnDestroyExcess += WorldSpritePoolDestroyed;
		}

		private void InitializeStructures()
		{
			_activeObjects = new Dictionary<int, WorldObject>();
			_activeSprites = new Dictionary<int, WorldSprite>();
			_activeChunks = new Dictionary<int, WorldChunk>();
			_currentChunkObstacles = new HashSet<float>();

			PrimarySpritePooler.Initialize();
		}

		private void InitializeRandomDistributions()
		{
			_obstacleRnd = new System.Random(ObstacleSeed);
			_outerObstacleRnd = new System.Random(ObstacleSeed);
			_icePathRnd = new System.Random(ObstacleSeed);

			_distributionObstacle = new DDRandom(Assets.DefaultObstacleWeights, _obstacleRnd);
			_distributionOuterObstacle = new DDRandom(Assets.DefaultOuterObstacleWeights, _outerObstacleRnd);
			_distributionIcePath = new DDRandom(Assets.DefaultIcePathWeights, _icePathRnd);
		}

		/// <summary> Initializes a newly created WorldSprite. </summary>
		private void InitializeSpriteObject(WorldSprite worldSprite, Sprite sprite, int order = 0)
		{
			worldSprite.SetSortingOrder(order);
			worldSprite.SetSprite(sprite);
		}

		/// <summary> Initializes a newly created Unity GameObject. </summary>
		private void InitializeGameObject(GameObject obj, Vector3 pos, Transform parent)
		{
			obj.transform.position = pos;
			obj.transform.SetParent(parent);
			obj.SetActive(true);
		}

		/// <summary> Initializes a WorldObject given an initialized GameObject and WorldSprite. </summary>
		private void InitializeWorldObject(int uid, GameObject gameObj, WorldSprite sprite, WorldObjectAction worldDestroyCallback)
		{
			// initialize props
			WorldObjectProps props = new WorldObjectProps(uid, gameObj, sprite);
			WorldMovementProps movement = new WorldMovementProps(_worldDirection, WorldDestroyY, WorldGenYHeight);

			// add to structures
			_activeObjects.Add(uid, new WorldObject(props, movement));
			_activeSprites.Add(uid, sprite);
			_spriteIds.Add(uid);

			// subscribe to events
			OnUpdate += _activeObjects[uid].UpdateObject;
			OnUpdateWorldMovement += _activeObjects[uid].UpdateObjectMovement;
			_activeObjects[uid].OnExitMapBoundaries += worldDestroyCallback;
		}

		/// <summary> Initializes a newly created PathChunk. </summary>
		private void InitializeChunk(WorldChunk chunk, WorldChunkAction destroyCallback)
		{
			// initialize props
			int uid = chunk.GetHashCode() + _activeChunks.Count;
			WorldMovementProps movement = new WorldMovementProps(_worldDirection, WorldDestroyY, WorldGenYHeight);
			chunk.UID = uid;
			chunk.SetMovement(movement);

			// add to structures
			_activeChunks.Add(uid, chunk);
			_chunkIds.Add(uid);

			// subscribe to events
			OnUpdate += _activeChunks[chunk.UID].UpdateChunk;
			OnUpdateWorldMovement += _activeChunks[uid].UpdateChunkMovement;
			_activeChunks[uid].OnDestroy += destroyCallback;

			// cache this uid as the last uid created
			_lastChunkID = uid;
		}

		/// <summary> Callback to a chunks reached height event. Generates the next forward chunk. </summary>
		private void WorldAutoGenerate(WorldChunk chunk)
		{
			chunk.ReachHeightThresholdUnsubscribe(WorldAutoGenerate);
			GenerateForward();
		}

		private void WorldSpritePoolAdded(WorldSprite sprite)
		{
			sprite.UID = sprite.GetHashCode() + PrimarySpritePooler.Count;
			sprite.gameObject.SetActive(false);
			sprite.name = $"PooledSprite[{sprite.UID}]";
		}

		private void WorldSpritePoolReturned(WorldSprite sprite)
		{
			sprite.SetParent(PrimarySpritePooler.PoolParent);
			sprite.gameObject.SetActive(false);
			sprite.name = $"PooledSprite[{sprite.UID}]";
			sprite.ResetSprite();
		}

		private void WorldSpritePoolDestroyed(WorldSprite sprite)
		{
			Destroy(sprite);
		}

		/// <summary> Generates a random obstacle at the given position with DDRandom. </summary>
		private void CheckGenerateObstacle(Vector3 pos, DDRandom ranDistribution)
		{
			int ran = ranDistribution.Next();

			// 0 is no obstacle
			if (ran == 0)
				return;

			// if this obstacle is extended, make sure it can fit
			if (Assets.Obstacles[ran].ExtendedObstacle)
			{
				if ((pos.x + 1) >= _activeChunks[_lastChunkID].RightEdge.transform.position.x)
					return;
				if (_currentChunkObstacles.Contains(pos.x + 1))
					return;
			}

			// if no obstacle there already, create one
			if (!_currentChunkObstacles.Contains(pos.x))
			{
				CreateObstacle(ran, pos);
			}
		}

		private void GenerateRowObstacle(int chunkID)
		{
			// generate the obstacles on the chunks center
			for (int j = 0; j < _activeChunks[chunkID].CenterSprites.Count; j++)
			{
				Vector3 colPos = _activeChunks[chunkID].CenterSprites[j].position;
				CheckGenerateObstacle(colPos, _distributionObstacle);
			}
		}

		private void GenerateRowOuterObstacle(float yPos)
		{
			// generate the obstacles on outer left
			for (int j = -WorldEdgeStart + 1; j < -_activeChunks[_lastChunkID].Thickness; j++)
			{
				Vector3 colPos = new Vector3(j, yPos, 0);
				CheckGenerateObstacle(colPos, _distributionOuterObstacle);
			}

			// generate the obstacles on outer right
			for (int j = _activeChunks[_lastChunkID].Thickness + 1; j < WorldEdgeStart; j++)
			{
				Vector3 colPos = new Vector3(j, yPos, 0);
				CheckGenerateObstacle(colPos, _distributionOuterObstacle);
			}
		}

		private void GenerateRowLevelEdge(float yPos)
		{
			Vector3 edgeTreePos = new Vector3(0, yPos, 0);
			edgeTreePos.x = WorldEdgeStart;
			CreateObstacle(3, edgeTreePos);
			edgeTreePos.x = -WorldEdgeStart;
			CreateObstacle(3, edgeTreePos);
		}

		/// <summary> Randomizes the next path to stay the same or expand or contract. </summary>
		private void RandomizeIcePath()
		{
			if (_activeChunks[_lastChunkID].PossibleChunks.Count == 0)
				return;

			int id = _distributionIcePath.Next();
			List<string> chunks = new();

			if (id == 0) // stay same width
			{
				chunks = _activeChunks[_lastChunkID].PossibleChunks;
			}
			else if(id == 1) // expand path
			{
				chunks = _activeChunks[_lastChunkID].PossibleExpandChunks;
			}
			else if(id == 2) // contract path
			{
				chunks = _activeChunks[_lastChunkID].PossibleContractChunks;
			}

			if(chunks.Count == 0)
			{
				chunks = _activeChunks[_lastChunkID].PossibleChunks;
			}

			_currWidthChunkName = chunks[UnityEngine.Random.Range(0, chunks.Count - 1)];
		}

		/// <summary> Creates and initializes a WorldTile given sprite props and returns the WorldSprite.  </summary>
		private bool CreateWorldObject(WorldSpriteProps objectProps, WorldObjectAction destroyCallback, out WorldSprite sprite)
		{
			// get sprite from pool			
			bool poolSuccess = PrimarySpritePooler.GetFromPool(out sprite);
			if (!poolSuccess)
			{
				Debug.LogError($"World CreateWorldObject failed. GetFromPool call returned false.");
				return false;
			}

			// initialize new sprite from props
			InitializeSpriteObject(sprite, objectProps.Sprite, objectProps.Order);

			// initialize game object from props
			InitializeGameObject(sprite.gameObject, objectProps.Position, objectProps.Parent);

			// initialize world object from sprite and game object
			InitializeWorldObject(sprite.UID, sprite.gameObject, sprite, destroyCallback);
			return true;
		}

		/// <summary> Create and initialize a chunk at position given width id. </summary>
		private void CreateChunk(Vector3 pos, string id)
		{
			if (!Assets.PathChunkPrefabs.ContainsKey(id))
			{
				Debug.LogError($"Cannot CreateChunk(). Chunk {id} does not exist.");
				return;
			}

			WorldChunk chunk = Instantiate(Assets.PathChunkPrefabs[id], pos, Quaternion.identity, ActiveChunkParent);
			InitializeChunk(chunk, DestroyChunk);
		}

		private void CreateObstacle(int obstacleID, Vector3 pos)
		{
			// create new obstacle data and sprite props
			Obstacle obstacle = Assets.Obstacles[obstacleID];
			WorldSpriteProps objProps = new(pos + obstacle.Offset, Assets.GetSprite(obstacle.SpriteID), obstacle.SortID, ActiveObjectParent);

			// try create sprite
			WorldSprite sprite;
			bool spriteSuccess = CreateWorldObject(objProps, DestroyObstacleObject, out sprite);

			if(!spriteSuccess)
			{
				Debug.LogError($"World CreateObstacle failed. CreateWorldObject call returned false.");
				return;
			}

			// set the sprite obstacle data
			sprite.SetObstacleData(obstacle);

			// subscribe sprite events
			sprite.CreateSFX = Assets.CreateSFX;
			sprite.CreatePFX = CreatePFXSprite;

			// add animator
			if (obstacle.AnimatorControllerID != 0)
			{
				sprite.EnableAnimator(Assets.AnimatorControllers[obstacle.AnimatorControllerID]);
			}

			// check for destroy on collision event subscribe
			if (obstacle.DestroyOnCollision)
			{
				sprite.OnObjectCollide += DestroyObstacleObject;
			}

			// check for extended obstacle
			if (obstacle.ExtendedObstacle)
			{
				float colPosX = pos.x + 1;
				CreateObstacle(obstacle.ExtendedObstacleID, new Vector3(colPosX, pos.y, pos.z));
				_currentChunkObstacles.Add(colPosX);
			}

			// add obstacle x position
			_currentChunkObstacles.Add(pos.x);
		}

		/// <summary> Callback for when a chunk should be destroyed. </summary>
		private void DestroyChunk(WorldChunk chunk)
		{
			// unsub events
			OnUpdate -= _activeChunks[chunk.UID].UpdateChunk;
			OnUpdateWorldMovement -= _activeChunks[chunk.UID].UpdateChunkMovement;
			_activeChunks[chunk.UID].OnDestroy -= DestroyChunk;

			// remove chunk
			_activeChunks.Remove(chunk.UID);

			// destroy chunk
			Destroy(chunk.gameObject);
		}

		/// <summary> Destroy base WorldObject given id. </summary>
		private void DestroyWorldObject(int id)
		{
			// unsub events
			OnUpdate -= _activeObjects[id].UpdateObject;
			OnUpdateWorldMovement -= _activeObjects[id].UpdateObjectMovement;

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
			int id = obj.GetId();

			// unsub events
			_activeObjects[id].OnExitMapBoundaries -= DestroyObstacleObject;
			_activeObjects[id].GetSprite().OnObjectCollide -= DestroyObstacleObject;
			_activeObjects[id].GetSprite().CreateSFX = null;
			_activeObjects[id].GetSprite().CreatePFX = null;

			// call base
			DestroyWorldObject(id);
		}

		/// <summary> Destroy obstacle given WorldObject id. </summary>
		/// <param name="uid"> The UID of the object to destroy. </param>
		private void DestroyObstacleObject(int id)
		{
			if (!_activeObjects.ContainsKey(id))
				return;

			// unsub events
			_activeObjects[id].GetSprite().OnObjectCollide -= DestroyObstacleObject;
			_activeObjects[id].OnExitMapBoundaries -= DestroyObstacleObject;

			// call base
			DestroyWorldObject(id);
		}

		/// <summary> Updates the distance travelled and other metrics given speed </summary>
		/// <param name="speed"> The speed to update by. </param>
		private void UpdateDistanceTravelled(float speed)
		{
			// update distance travelled
			_distanceTravelled += speed;
			OnUpdateDistanceTravelled.Invoke(_distanceTravelled);

			// mps is the difference in distance travelled * 50 (fixed update 0.02s, which is 50 per sec)
			_metersPerSecond = (_distanceTravelled - _lastDistance) * 50;

			// 1 mps = 3.6 kmh
			_kmh = _metersPerSecond * 3.6f;
			OnUpdateKMH.Invoke(_kmh);

			// last distance is now the distance travelled
			_lastDistance = _distanceTravelled;
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			InitializeStructures();
		}

		private void Start()
		{
			InitializeEvents();
			InitializeRandomDistributions();
		}

		private void Update()
		{
			OnUpdate.Invoke();
		}

		private void FixedUpdate()
		{
			OnUpdateWorldMovement.Invoke(WorldSpeed);
		}

		#endregion
	}
}


