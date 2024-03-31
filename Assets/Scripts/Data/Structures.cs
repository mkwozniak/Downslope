using System.Collections.Generic;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;
using InputField = TMPro.TMP_InputField;
using ChunkPlacementStructure = System.Collections.Generic.Dictionary<float, Wozware.Downslope.LevelCreatorPlaceable>;

namespace Wozware.Downslope
{
	/// <summary>
	/// Properties to describe the sprite of a world object.
	/// </summary>
	public struct WorldSpriteProps
	{
		public Vector3 Position;
		public Transform Parent;
		//public Sprite Sprite;
		public SpriteAnimation Sprite;
		public SpriteLayerSortData SortData;

		public WorldSpriteProps(Vector3 pos, SpriteAnimation spr, SpriteLayerSortData sortData, Transform parent)
		{
			Position = pos;
			Sprite = spr;
			SortData = sortData;
			Parent = parent;
		}
	}

	/// <summary>
	/// Properties to describe a complete world object.
	/// </summary>
	public struct WorldObjectProps
	{
		public int GenID;
		public GameObject Obj;

		public WorldObjectProps(int id, GameObject obj)
		{
			GenID = id;
			Obj = obj;
		}
	}

	/// <summary>
	/// Properties to describe an objects vertical world movement.
	/// </summary>
	public struct WorldMovementProps
	{
		public Vector3 Direction;
		public float MaxY;
		public float HeightThresholdY;

		public WorldMovementProps(Vector3 direction, float maxY, float genYHeight)
		{
			Direction = direction;
			MaxY = maxY;
			HeightThresholdY = genYHeight;
		}
	}

	/// <summary>
	/// Represents threshold with sprite for height shadows.
	/// </summary>
	[System.Serializable]
	public struct HeightShadowThreshold
	{
		public Sprite ShadowSprite;
		public float Threshold;
	}

	/// <summary>
	/// Represents an Obstacles fundamental data.
	/// </summary>
	[System.Serializable]
	public struct Obstacle
	{
		public string Name;
		public int ParentID;

		public string SpriteID;
		public int AnimatorControllerID;
		public Vector3 Offset;
		public bool Obstrusive;

		public bool Impassable;
		public CollisionTypes CollisionType;
		public ObstacleColliderData ColliderData;
		public bool HasCollisionSpeedPenalty;
		public float CollisionSpeedPenalty;
		public bool SFXOnCollision;
		public string CollisionSFXID;
		public bool PFXOnCollision;
		public string CollisionPFXID;

		public bool IsCenterCollidable;
		public float CenterCollisionDistance;
		public float CenterCollisionSpeedPenalty;
		public bool StunOnCenterCollision;
		public float StunDuration;
		public bool SFXOnCenterCollision;
		public string CenterCollisionSFXID;
		public bool PFXOnCenterCollision;
		public string CenterCollisionPFXID;

		public bool IsRamp;
		public bool IsSoftJump;
		public float ForwardRampPower;
		public float VerticalRampPower;
		public float VerticalRampMax;

		public bool AirCollidable;
		public bool OnlyAirCollidable;
		public bool DestroyOnCollision;
		public bool RightExtendedObstacle;
		public List<string> RightExtendedObstacles;
		public bool TopExtendedObstacle;
		public List<string> TopExtendedObstacles;

		public int SizeLOSX;
		public int SizeLOSY;
	}

	/// <summary>
	/// Represents an Obstacles collider size and offset.
	/// </summary>
	[System.Serializable]
	public struct ObstacleColliderData
	{
		public string Name;
		public Vector2 Size;
		public Vector2 Offset;
	}


	/// <summary>
	/// Represents a sprites animation frame.
	/// </summary>
	[System.Serializable]
	public struct SpriteAnimationFrame
	{
		public float FrameTime;
		public Sprite Sprite;
	}

	/// <summary>
	/// Represents a sprites animation.
	/// </summary>
	[System.Serializable]
	public struct SpriteAnimation
	{
		public string Name;
		public List<SpriteAnimationFrame> DefaultAnimation;
		public List<SpriteAnimationFrame> HitAnimation;
		public List<SpriteAnimationFrame> DestroyedAnimation;
	}

	/// <summary>
	/// Represents a Unity sprite.
	/// </summary>
	[System.Serializable]
	public struct SpriteData
	{
		public string Name;
		public Sprite Sprite;
	}

	/// <summary>
	/// Represents a list of possible Unity Audioclips for SFX.
	/// </summary>
	[System.Serializable]
	public struct SFXData
	{
		public string Name;
		public List<AudioClip> Clips;
	}

	/// <summary>
	/// Represents a list of possible Unity ParticleFX.
	/// </summary>
	[System.Serializable]
	public struct PFXData
	{
		public string Name;
		public List<ParticleSystem> FX;
	}

	/// <summary>
	/// Represents a list of possible Unity ParticleFX.
	/// </summary>
	[System.Serializable]
	public struct SlopeAngleData
	{
		public SlopeAngleTypes AngleType;
		public float ForwardSpeed;
		public float ForwardAccel;
	}

	/// <summary>
	/// Represents all map types base identity.
	/// </summary>
	[System.Serializable]
	public struct MapIdentity
	{
		public SlopeAngleTypes AngleType;
		public int AngleOffset;
	}

	/// <summary>
	/// Represents an Obstacles random distribution weight.
	/// </summary>
	[System.Serializable]
	public struct WeightIdentity
	{
		public string Name;
		public int Weight;
	}

	/// <summary>
	/// Represents the data for random weights of each map component.
	/// </summary>
	[System.Serializable]
	public struct WeightedMapData
	{
		// name is outside identity for visual clarity in Unity Inspector
		public string Name;
		public int MusicID;
		public MapIdentity Identity;
		public int MaxIcePathWidth;
		public int WorldEdgeSize;
		public int MinWorldEdgeSize;
		public List<WeightIdentity> ObstacleWeights;
		public List<WeightIdentity> IceObstacleWeights;
		public List<WeightIdentity> SnowVariationWeights;
		public List<WeightIdentity> IcePathWeights;
		public List<WeightIdentity> WorldEdgeWeights;
		public List<WeightIdentity> WorldXOffsetWeights;
		public List<WeightIdentity> IcePathSpawnWeights;
	}

	/// <summary>
	/// Represents tutorial stage data.
	/// </summary>
	[System.Serializable]
	public struct TutorialStage
	{
		[Multiline] public string Message;
		public string Objective;
	}

	/// <summary>
	/// Represents compact UI tutorial data.
	/// </summary>
	[System.Serializable]
	public struct TutorialUI
	{
		public GameObject RootUIView;
		public GameObject MessageView;
		public GameObject ObjectiveView;
		public Text MessageLabel;
		public Text ObjectiveLabel;
		public int InitialStageThreshold;
		public List<TutorialStage> TutorialStages;
	}

	/// <summary>
	/// Represents compact UI map data.
	/// </summary>
	[System.Serializable]
	public struct MapUI
	{
		public GameObject RootView;
		public GameObject Tilemap;
		public List<WorldMapPoint> MapPoints;
	}

	/// <summary>
	/// Represents compact UI level creator data.
	/// </summary>
	[System.Serializable]
	public struct LevelCreatorUI
	{
		public Transform RootView;
		public Transform LayerChoicesPanel;
		public Transform LayerObjectChoicesPanel;
		public Transform ChoiceParent;
		public Transform SavePanel;
		public Transform SaveEntryContentParent;
		public Transform SaveInputPanel;
		public Transform InfoPanel;
		public Text SavePanelTitleLabel;
		public Text InfoTipLabel;
		public Transform TopMessagePanel;
		public Text TopMessageLabel;
		public InputField SaveFileInputField;
		public InputField SaveNameInputField;
		public Text ChunkRangeLabel;
	}

	/// <summary>
	/// Represents a level creator placeable object
	/// </summary>
	[System.Serializable]
	public struct LevelCreatorPlaceable
	{
		public string DisplayName;
		public string SpriteID;
		public string PlacementID;
		public string IconID;
		public SpriteLayerTypes LayerType;
		public LevelCreatorObjectTypes ObjectType;
	}

	public struct SpriteLayerSortData
	{
		public string LayerName;
		public int LayerID;

		public SpriteLayerSortData(string layerName, int layerId)
		{
			LayerName = layerName;
			LayerID = layerId;
		}
	}

	public sealed class LevelObjectData
	{
		public int UID;
		public LevelCreatorObject Obj;
		public LevelCreatorPlaceable Placeable;
	}

	[System.Serializable]
	public sealed class LevelChunkData
	{
		public int DistanceID;
		public int EdgeSize;
		public Dictionary<SpriteLayerTypes, ChunkPlacementStructure> PlacementData = new();
	}

	/// <summary>
	/// Represents a map file object
	/// </summary>
	[System.Serializable]
	public struct MapFileData
	{
		public string Name;
		public string FileName;
		public string Path;
		public string Time;
		public Dictionary<uint, LevelChunkData> ChunkData;

		public MapFileData(string name, string fileName, string path, string time, Dictionary<uint, LevelChunkData> chunkData)
		{
			Name = name;
			FileName = fileName;
			ChunkData = chunkData;
			Path = path;
			Time = time;
		}
	}

	public sealed class ActiveIcePathData
	{
		public bool NotYetStarted = false;
		public bool IsEnding = false;
		public bool IsTurning = false;
		public string TurnDirection = "Right";
		public float XPosition = 0;
		public float Thickness = 0;
		public Vector3 Position;
		public string IcePathID = "None";
		public int SizeId = 0;
		public List<float> Occupations = new();
		public HashSet<float> NewOccupations = new();

		public ActiveIcePathData(bool notYetStarted, bool isEnding, float xPosition, float thickness, Vector3 position, string icePathID, List<float> occupations)
		{
			NotYetStarted = notYetStarted;
			IsEnding = isEnding;
			XPosition = xPosition;
			Thickness = thickness;
			Position = position;
			IcePathID = icePathID;
			Occupations = occupations;
			NewOccupations = new();
		}
	}

	public sealed class WorldWeightedRandomLayerData
	{
		public System.Random Rand
		{
			get { return _rnd;}
			set { _rnd = value; }
		}

		public DDRandom Distribution
		{
			get { return _distribution; }
			set { _distribution = value; }
		}

		public List<string> Possibilities
		{
			get { return _possibilities; }
			set { _possibilities = value; }
		}

		private System.Random _rnd;
		private DDRandom _distribution;
		private List<string> _possibilities = new List<string>();
	}
}