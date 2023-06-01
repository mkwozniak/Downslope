using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	/// <summary>
	/// Properties to describe the sprite of a world object.
	/// </summary>
	public struct WorldSpriteProps
	{
		public Vector3 Position;
		public Transform Parent;
		public Sprite Sprite;
		public int Order;

		public WorldSpriteProps(Vector3 pos, Sprite spr, int order, Transform parent)
		{
			Position = pos;
			Sprite = spr;
			Order = order;
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
		public WorldSprite Sprite;

		public WorldObjectProps(int id, GameObject obj, WorldSprite sprite)
		{
			GenID = id;
			Obj = obj;
			Sprite = sprite;
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
	/// Represents an Obstacles fundamental data.
	/// </summary>
	[System.Serializable]
	public struct Obstacle
	{
		public string Name;
		public int ParentID;

		public string SpriteID;
		public int SortID;
		public string SortingLayer;
		public int AnimatorControllerID;
		public Vector3 Offset;

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
		public float ForwardRampPower;
		public float VerticalRampPower;
		public float VerticalRampMax;
		public bool IsSoftJump;

		public bool AirCollidable;
		public bool DestroyOnCollision;
		public bool ExtendedObstacle;
		public int ExtendedObstacleID;
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
	/// Represents an Obstacles random distribution weight.
	/// </summary>
	[System.Serializable]
	public struct ObstacleWeight
	{
		public string Name;
		public int Weight;
	}
}