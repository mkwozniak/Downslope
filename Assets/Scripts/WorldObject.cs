using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wozware.Downslope
{
	public sealed class WorldObject
	{
		public WorldObjectAction OnExitMapBoundaries;

		private Vector3 _velocity;
		private WorldObjectProps _props;
		private WorldMovementProps _movement;
		private bool _destroyed;

		public WorldObject(WorldObjectProps props, WorldMovementProps movement)
		{
			_props = props;
			_movement = movement;
		}

		public GameObject GetObj()
		{
			return _props.Obj;
		}

		public int GetId()
		{
			return _props.GenID;
		}

		public WorldSprite GetSprite()
		{
			return _props.Sprite;
		}

		public void SetParent(Transform transform)
		{
			_props.Obj.transform.SetParent(transform);
		}

		public void UpdateObjectMovement(float speed)
		{
			if (_destroyed)
				return;

			_velocity.x = _movement.Direction.x * speed;
			_velocity.y = _movement.Direction.y * speed;

			_props.Obj.transform.position += _velocity;
		}

		public void UpdateObject()
		{
			if (_props.Obj.transform.position.y > _movement.MaxY && !_destroyed)
			{
				OnExitMapBoundaries.Invoke(this);
				_destroyed = true;
			}
		}
	}
}

