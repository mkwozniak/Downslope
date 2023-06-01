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
		public GameObject PowderSnowFX;
		public GameObject SmallTreeSnowFX;

		public List<Sprite> SpriteList;
		public List<Obstacle> Obstacles;
		public List<int> DefaultObstacleWeights;
		public List<int> DefaultOuterObstacleWeights;
		public List<int> DefaultIcePathWeights;
		public List<AudioClip> AmbientSounds;
		public List<SFXData> SFXList;
		public List<PFXData> PFXList;

		public Dictionary<string, Sprite> Sprites;
		public Dictionary<CollisionTypes, GameObject> CollidePlayerFX;
		public Dictionary<string, WorldChunk> PathChunkPrefabs;
		public Dictionary<string, ObstacleColliderData> ObstacleColliders;
		public Dictionary<string, SFXData> SFX;
		public Dictionary<string, PFXData> PFX;
		public Dictionary<int, Sprite> PlayerTrailSprites;

		[SerializeField] private List<WorldChunk> _pathChunkPrefabs;
		[SerializeField] private List<Sprite> _playerTrailSprites;

		public void Initialize()
		{
			Sprites = new Dictionary<string, Sprite>();
			SFX = new Dictionary<string, SFXData>();
			PFX = new Dictionary<string, PFXData>();
			CollidePlayerFX = new Dictionary<CollisionTypes, GameObject>();
			PathChunkPrefabs = new Dictionary<string, WorldChunk>();
			ObstacleColliders = new Dictionary<string, ObstacleColliderData>();
			PlayerTrailSprites = new Dictionary<int, Sprite>();

			int i = 0;

			for (i = 0; i < _pathChunkPrefabs.Count; i++)
			{
				PathChunkPrefabs.Add(_pathChunkPrefabs[i].name, _pathChunkPrefabs[i]);
			}

			for (i = 0; i < SpriteList.Count; i++)
			{
				Sprites.Add(SpriteList[i].name, SpriteList[i]);
			}

			for (i = 0; i < SFXList.Count; i++)
			{
				SFX.Add(SFXList[i].Name, SFXList[i]);
			}

			for (i = 0; i < PFXList.Count; i++)
			{
				PFX.Add(PFXList[i].Name, PFXList[i]);
			}

			for (i = 0; i < _playerTrailSprites.Count; i++)
			{
				PlayerTrailSprites.Add(i, _playerTrailSprites[i]);
			}

		}

		public Sprite GetSprite(string id)
		{
			if (!Sprites.ContainsKey(id))
				return Sprites["empty"];

			return Sprites[id];
		}

		public AudioClip GetAmbientClip(int id)
		{
			if (id >= SFXList.Count || id < 0)
				return AmbientSounds[0];

			return AmbientSounds[id];
		}

		public void CreateSFX(string id, Vector3 pos)
		{
			if (!SFX.ContainsKey(id))
				return;

			AudioSource source = Instantiate(SFXPrefab, pos, Quaternion.identity);
			int ranClip = Random.Range(0, SFX[id].Clips.Count);
			source.clip = SFX[id].Clips[ranClip];
			source.Play();

			Destroy(source.gameObject, source.clip.length + 0.1f);
		}

		public bool TryCreatePFX(string id, Vector3 pos, out ParticleSystem pfxOut, UnityEngine.Transform parent = null)
		{
			if (!PFX.ContainsKey(id))
			{
				Debug.LogError($"AssetPack CreatePFX does not contain id: {id}. Returning null.");
				pfxOut = null;
				return false;
			}

			int ranFX = Random.Range(0, PFX[id].FX.Count);
			pfxOut = Instantiate(PFX[id].FX[ranFX], pos, Quaternion.identity, parent);
			return true;
		}
	}
}



