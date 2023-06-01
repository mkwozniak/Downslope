using UnityEngine;

namespace Wozware.Downslope
{
	public delegate void WorldObjectAction(WorldObject t);
	public delegate void WorldChunkAction(WorldChunk t);

	public delegate void WorldFrameUpdating();
	public delegate void WorldMovementUpdating(float speed);
	public delegate void SpeedUpdating(float val);
	public delegate void DistanceUpdating(float val);

	public delegate void ObjectColliding(int uid);
	public delegate void WorldSpriteColliding(WorldSprite spr);
	public delegate void IceColliding(Vector3 pos, int objectId);
	public delegate void PowderColliding(Vector3 pos);
	public delegate void JumpLanding(Vector3 pos);

	public delegate void ObstacleCreating(Vector3 pos);
	public delegate void FXCreating(string id, Vector3 pos);

	public delegate GameState GameStateReturning();
	public delegate AudioClip AudioClipReturning(int id);
	public delegate float SpeedReturning();
}