using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace Wozware.Downslope
{
	[ExecuteInEditMode]
	public class TilemapRandomizer : MonoBehaviour
	{
		public bool Randomize = false;
		public bool Clear = false;
		public bool ClearConfirm = false;

		public Tilemap Map;
		public List<TileBase> RandomTiles = new();
		public Vector2Int Size;
		public Vector2Int Offset;

		public void Update()
		{
			if(Randomize)
			{
				RandomizeTilemap();
				Randomize = false;
			}

			if (!Clear && ClearConfirm)
			{
				Clear = false;
				ClearConfirm = false;
				return;
			}

			if (Clear)
			{
				if(ClearConfirm)
				{
					ClearTilemap();
					Clear = false;
					ClearConfirm = false;
				}
			}
		}

		public void ClearTilemap()
		{
			if (Map == null)
			{
				Debug.LogError("Tilemap reference is null.");
				return;
			}

			Map.ClearAllTiles();
			Debug.Log("Map Cleared Of All Tiles");
		}

		public void RandomizeTilemap()
		{
			if(Map == null)
			{
				Debug.LogError("Tilemap reference is null.");
				return;
			}
			if(RandomTiles.Count == 0)
			{
				Debug.LogError("No Tiles in random list.");
				return;
			}

			if(Size.x <= 0 || Size.y <= 0)
			{
				Debug.LogError("Size cannot be less than or 0.");
				return;
			}

			for(int y = 0; y < Size.y; y++)
			{
				for (int x = 0; x < Size.y; x++)
				{
					Map.SetTile(new Vector3Int(x + Offset.x, y + Offset.y, 0), RandomTiles[Random.Range(0, RandomTiles.Count)]);
				}
			}

			Debug.Log("Map Randomized.");
		}
	}
}

