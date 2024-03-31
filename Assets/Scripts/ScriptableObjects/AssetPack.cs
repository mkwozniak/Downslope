using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Wozware.Downslope
{
	[CreateAssetMenu(fileName = "AssetPack", menuName = "Downslope/AssetPack", order = 1)]
	public sealed class AssetPack : ScriptableObject
	{
		public List<RuntimeAnimatorController> AnimatorControllers;
		public AudioSource SFXPrefab;
		public WorldChunk ChunkPrefab;
		public GameObject PowderSnowFX;
		public GameObject SmallTreeSnowFX;
		public UIElement UI_LevelCreatorPlacementChoice;
		public UIMapSaveEntry UI_LevelCreatorSaveEntry;
		public UIMapSaveEntry UI_LevelCreatorLoadEntry;
		public LevelCreatorObject LevelCreator_PlacementObject;

		public List<Obstacle> Obstacles;
		public List<AudioClip> Music;
		public List<AudioClip> AmbientSounds;
		public List<SFXData> SFXList;
		public List<PFXData> PFXList;

		[Header("Level Creator Data")]
		public List<LevelCreatorPlaceable> LevelCreatorPlaceables;

		public Dictionary<CollisionTypes, GameObject> CollidePlayerFX;
		public Dictionary<string, WeightedMapData> ArcadeModeMaps;
		public Dictionary<string, WorldIcePath> IcePathStartingPrefabs;
		public Dictionary<string, WorldIcePath> IcePathPrefabs;
		public Dictionary<string, ObstacleColliderData> ObstacleColliders;
		public Dictionary<string, SFXData> SFX;
		public Dictionary<string, PFXData> PFX;
		public Dictionary<int, Sprite> PlayerTrailSprites;
		public Dictionary<string, Obstacle> ObstacleIDs;

		[SerializeField] private List<WorldIcePath> _icePathStartingPrefabs;
		[SerializeField] private List<WorldIcePath> _icePathPrefabs;
		[SerializeField] private List<Sprite> _playerTrailSprites;

		public void Initialize()
		{
			SFX = new();
			PFX = new();
			ObstacleIDs = new();
			ArcadeModeMaps = new();
			CollidePlayerFX = new();
			IcePathPrefabs = new();
			IcePathStartingPrefabs = new();
			ObstacleColliders = new();
			PlayerTrailSprites = new();

			int i;
			for (i = 0; i < _icePathStartingPrefabs.Count; i++)
			{
				IcePathStartingPrefabs.Add(_icePathStartingPrefabs[i].name, _icePathStartingPrefabs[i]);
			}

			for (i = 0; i < _icePathPrefabs.Count; i++)
			{
				IcePathPrefabs.Add(_icePathPrefabs[i].name, _icePathPrefabs[i]);
			}

			for (i = 0; i < Obstacles.Count; i++)
			{
				ObstacleIDs.Add(Obstacles[i].Name, Obstacles[i]);
			}

			for (i = 0; i < SFXList.Count; i++)
			{
				SFX.Add(SFXList[i].Name, SFXList[i]);
			}
		}

		public AudioClip GetAmbientClip(int id)
		{
			if (id >= SFXList.Count || id < 0)
				return AmbientSounds[0];

			return AmbientSounds[id];
		}

		public bool TryGetSFX(string id, out AudioClip clip)
		{
			if (!SFX.ContainsKey(id))
			{
				Debug.LogError($"TryGetSFX: Sound {id} does not exist. Out is null.");
				clip = null;
				return false;
			}

			int ranClip = Random.Range(0, SFX[id].Clips.Count);
			clip = SFX[id].Clips[ranClip];
			return true;
		}

		public bool TryCreatePFX(string id, Vector3 pos, out ParticleSystem pfxOut, UnityEngine.Transform parent = null)
		{
			if (!PFX.ContainsKey(id))
			{
				Debug.LogWarning($"AssetPack: TryCreatePFX does not contain id: {id}. Out is null.");
				pfxOut = null;
				return false;
			}

			int ranFX = Random.Range(0, PFX[id].FX.Count);
			pfxOut = Instantiate(PFX[id].FX[ranFX], pos, Quaternion.identity, parent);
			return true;
		}
	}
}



