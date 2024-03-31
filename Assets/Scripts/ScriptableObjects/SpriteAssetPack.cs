using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wozware.Downslope;

[CreateAssetMenu(fileName = "SpriteAssetPack", menuName = "Downslope/SpriteAssetPack", order = 3)]
public sealed class SpriteAssetPack : ScriptableObject
{
	public SpriteAnimation ErrorSprite;
	public List<SpriteAnimation> ObstacleSpriteList;
	public List<SpriteAnimation> GroundSpriteList;
	public List<SpriteAnimation> ArchitectureSpriteList;
	public Dictionary<string, SpriteAnimation> AllSprites;
	public Dictionary<string, SpriteAnimation> ObstacleSprites;
	public Dictionary<string, SpriteAnimation> GroundSprites;
	public Dictionary<string, SpriteAnimation> ArchitectureSprites;

	public void Initialize()
	{
		AllSprites = new();
		ObstacleSprites = new();
		GroundSprites = new();
		ArchitectureSprites = new();

		LoadSpritesFromList(ObstacleSpriteList, ObstacleSprites);
		LoadSpritesFromList(GroundSpriteList, GroundSprites);
		LoadSpritesFromList(ArchitectureSpriteList, ArchitectureSprites);
	}

	public SpriteAnimation GetSpriteAnimation(string id)
	{
		if(AllSprites.ContainsKey(id))
		{
			return AllSprites[id];
		}

		Debug.LogError($"GetSpriteAnimation '{id}' not found.");
		return ErrorSprite;
	}

	public Sprite GetSprite(string id)
	{
		if(AllSprites.ContainsKey(id))
		{
			return AllSprites[id].DefaultAnimation[0].Sprite;
		}

		Debug.LogError($"GetSprite '{id}' not found.");
		return ErrorSprite.DefaultAnimation[0].Sprite;
	}

	private void LoadSpritesFromList(List<SpriteAnimation> list, Dictionary<string, SpriteAnimation> map)
	{
		int i = 0;
		for (i = 0; i < list.Count; i++)
		{
			map.Add(list[i].Name, list[i]);
			AllSprites.Add(list[i].Name, list[i]);
		}
	}
}
