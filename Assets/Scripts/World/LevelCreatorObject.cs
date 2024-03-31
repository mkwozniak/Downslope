using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wozware.Downslope
{
	public sealed class LevelCreatorObject : MonoBehaviour
	{
		public Action<LevelCreatorObject> OnDestroyTriggered;
		public Action OnUpdate;

		public int UID;
		public SpriteRenderer Renderer;

		private WorldObjectProps _props;
		private bool _initialized = false;
		private bool _destroyed;

		public void Initialize(WorldObjectProps props)
		{
			_props = props;
			_initialized = true;
			_destroyed = false;
			OnUpdate = UpdateObject;
		}

		public GameObject GetObj()
		{
			return _props.Obj;
		}

		public int GetId()
		{
			return _props.GenID;
		}

		public void SetParent(Transform transform)
		{
			transform.SetParent(transform);
		}

		public void UpdateObject()
		{
			if (!_initialized)
			{
				return;
			}
		}

		public void TriggerDestroy()
		{
			OnDestroyTriggered.Invoke(this);
			_initialized = false;
			_destroyed = true;
		}

		public void Update()
		{
			UpdateObject();
		}

		public void FixedUpdate()
		{

		}
	}
}

