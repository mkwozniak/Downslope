using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{

	public static class FilePathData
	{
		public static string PERSISTENT_PATH = Application.persistentDataPath;
		public static string FOLDER_MAPS = "maps";
		public static string EXT_MAPS = "dsmap";
		public static string EXT_CNTRLS = "dsctrl";
		public static string FILENAME_CNTRLS = "custom_controls";
		public static HashSet<char> MAP_NAME_ILLEGAL_CHARACTERS = new HashSet<char>
		{
			'.', ',', '/', '\\', '"', ':', ';', ']', '[', '{', '}', '|', '^', '&', '`', '$', '@', '#', '-', '+', '<', '>', '@', '^', '~', '%',  
		};
	}

	public static class SpriteSortData
	{
		public static readonly Dictionary<SpriteLayerTypes, SpriteLayerSortData> LayerData = new Dictionary<SpriteLayerTypes, SpriteLayerSortData>
		{
			{SpriteLayerTypes.Ground, new SpriteLayerSortData("Ground", 0) },
			{SpriteLayerTypes.GroundPlus, new SpriteLayerSortData("Ground", 1) },
			{SpriteLayerTypes.Trail, new SpriteLayerSortData("Ground", 2) },
			{SpriteLayerTypes.Obstacle, new SpriteLayerSortData("Ground", 3) },
			{SpriteLayerTypes.Sky, new SpriteLayerSortData("Sky", 0) },
			{SpriteLayerTypes.Level, new SpriteLayerSortData("Objects", 100) },
		};
	}

	public static class PrefixID
	{
		public const string PATH_OBJECT_PREFIX = "Path";
		public const string OBSTACLE_OBJECT_PREFIX = "Obstacle";
	}

	public static class SpriteID
	{
		public const string EMPTY = "empty";
		public const string SMALL_SHRUB_0 = "small-shrub-0";
		public const string SMALL_TREE_0 = "small-tree-0";
		public const string LARGE_TREE_0 = "large-tree-0";
		public const string STUMP_0 = "stump-0";
		public const string ICE_EDGE_VERTICAL_LEFT = "ice-edge-vertical-left";
		public const string ICE_EDGE_VERTICAL_RIGHT = "ice-edge-vertical-right";
		public const string ICE_EDGE_HORIZONTAL_TOP = "ice-edge-vertical-right";
		public const string ICE_EDGE_HORIZONTAL_BOT = "ice-edge-vertical-right";
		public const string ICE_EDGE_EXPAND_TOP_LEFT = "ice-edge-vertical-right";
		public const string ICE_EDGE_EXPAND_TOP_RIGHT = "ice-edge-vertical-right";
		public const string ICE_EDGE_EXPAND_BOT_LEFT = "ice-edge-vertical-right";
		public const string ICE_EDGE_EXPAND_BOT_RIGHT = "ice-edge-vertical-right";
		public const string ICE_EDGE_CORNER_TOP_LEFT = "ice-edge-vertical-right";
		public const string ICE_EDGE_CORNER_TOP_RIGHT = "ice-edge-vertical-right";
		public const string ICE_EDGE_CORNER_BOT_LEFT = "ice-edge-vertical-right";
		public const string ICE_EDGE_CORNER_BOT_RIGHT = "ice-edge-vertical-right";
		public const string ICE_ARC_TOP_LEFT = "ice-edge-vertical-right";
		public const string ICE_ARC_TOP_RIGHT = "ice-edge-vertical-right";
		public const string ICE_ARC_BOT_LEFT = "ice-edge-vertical-right";
		public const string ICE_ARC_BOT_RIGHT = "ice-edge-vertical-right";
		public const string ICE_CURVE_TOP_LEFT = "ice-edge-vertical-right";
		public const string ICE_CURVE_TOP_RIGHT = "ice-edge-vertical-right";
		public const string ICE_CURVE_BOT_LEFT = "ice-edge-vertical-right";
		public const string ICE_CURVE_BOT_RIGHT = "ice-edge-vertical-right";

		public const string TRAIL_ICE_STRAIGHT = "trail-ice-straight";
		public const string TRAIL_ICE_CARVE = "trail-ice-carve";
		public const string TRAIL_ICE_AFTERCARVE = "trail-ice-aftercarve";
		public const string TRAIL_SNOW_STRAIGHT = "trail-snow-straight";
		public const string TRAIL_SNOW_CARVE = "trail-snow-carve";
		public const string TRAIL_SNOW_AFTERCARVE = "trail-snow-aftercarve";

		public static readonly Dictionary<TerrainTypes, Dictionary<TrailTypes, string>> TRAIL_IDS = 
			new Dictionary<TerrainTypes, Dictionary<TrailTypes, string>>
		{
			{ TerrainTypes.Powder, new Dictionary<TrailTypes, string>()
				{
					{ TrailTypes.Straight, TRAIL_SNOW_STRAIGHT },
					{ TrailTypes.Carve, TRAIL_SNOW_CARVE },
					{ TrailTypes.AfterCarve, TRAIL_SNOW_AFTERCARVE },
				} 
			},
			{ TerrainTypes.Ice, new Dictionary<TrailTypes, string>()
				{
					{ TrailTypes.Straight, TRAIL_ICE_STRAIGHT },
					{ TrailTypes.Carve, TRAIL_ICE_CARVE },
					{ TrailTypes.AfterCarve, TRAIL_ICE_AFTERCARVE },
				}
			},
		};
	}

	public static class SoundID
	{
		public const string CHALLENGE_SUCCESS = "ChallengeSuccess";
	}

	public static class WeightedRandomID
	{
		public const string ICE_PATH_PREFIX = "Path";

		public const string ICE_PATH_EMPTY = "Empty";
		public const string ICE_PATH_FLAT = "Flat";
		public const string ICE_PATH_EXPAND = "Expand";
		public const string ICE_PATH_CONTRACT = "Contract";
		public const string ICE_PATH_TURN = "Turn";
		public const string ICE_PATH_END = "End";

		public const string WORLD_TURN_FLAT = "Flat";
		public const string WORLD_TURN_RIGHT = "Right";
		public const string WORLD_TURN_LEFT = "Left";

		public const string WORLD_EDGE_FLAT = "Flat";
		public const string WORLD_EDGE_EXPAND = "Expand";
		public const string WORLD_EDGE_CONSTRACT = "Contract";

		public const string LAYER_OBSTACLE = "Obstacle";
		public const string LAYER_OBSTACLE_OUTER = "ObstacleOuter";
		public const string LAYER_ICE_PATH = "IcePath";
		public const string LAYER_SNOW_VARIATION = "SnowVariation";
		public const string LAYER_WORLD_EDGE = "WorldEdge";
		public const string LAYER_WORLD_XOFFSET = "WorldXOffset";

		public static readonly Dictionary<string, float> WORLD_EDGE_VARIATIONS = new Dictionary<string, float>()
		{
			{"Expand", 0.5f },
			{"Contract", -0.5f },
		};
	}

	public static class Collider2DExtensions
	{
		public static void TryUpdateShapeToAttachedSprite(this PolygonCollider2D collider)
		{
			collider.UpdateShapeToSprite(collider.GetComponent<SpriteRenderer>().sprite);
		}

		public static void UpdateShapeToSprite(this PolygonCollider2D collider, Sprite sprite)
		{
			// ensure both valid
			if (collider != null && sprite != null)
			{
				// update count
				collider.pathCount = sprite.GetPhysicsShapeCount();

				// new paths variable
				List<Vector2> path = new List<Vector2>();

				// loop path count
				for (int i = 0; i < collider.pathCount; i++)
				{
					// clear
					path.Clear();
					// get shape
					sprite.GetPhysicsShape(i, path);
					// set path
					collider.SetPath(i, path.ToArray());
				}
			}
		}
	}
}
