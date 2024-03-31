using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(WorldObject))]
	public sealed class WorldIcePath : MonoBehaviour
	{
		#region Events

		public event Action<WorldIcePath> OnDestroy;

		#endregion

		#region Public Members

		public float Thickness;
		public float XOffset;
		public int SizeID;

		public WorldObject WorldObj
		{
			get
			{
				return _worldObject;
			}
		}

		#endregion

		#region Private Members

		private WorldObject _worldObject;
		private bool _aboveAI;

		#endregion

		#region Unity Methods

		private void Awake()
		{
			_worldObject = GetComponent<WorldObject>();
		}

		private void Update()
		{
			UpdateChunk();
		}

		#endregion

		#region Public Methods

		public void Initialize(WorldObjectProps props, WorldMovementProps movement)
		{
			_worldObject.Initialize(props, movement, true);
			_worldObject.SelfUpdate += UpdateChunk;
			_worldObject.SelfFixedUpdate += _worldObject.UpdateObjectMovement;
		}

		public void SetMovement(WorldMovementProps props)
		{
			_worldObject.SetMovement(props);
		}

		public void UpdateChunk()
		{
			if(!_worldObject.Initialized)
			{
				return;
			}

			_aboveAI = WorldObject.IsObjectAboveAI.Invoke(transform.position.y);

			if (transform.position.y > _worldObject.Movement.MaxY && !_worldObject.Destroyed && (Game.WORLD_DESTROY || _aboveAI))
			{
				OnDestroy(this);
				_worldObject.SetAsDestroyed();
			}
		}

		public void TriggerDestroy()
		{
			_worldObject.SelfUpdate = null;
			_worldObject.SelfFixedUpdate = _worldObject.UpdateObjectMovement;
			_worldObject.SetAsDestroyed();
			OnDestroy(this);
		}

		#endregion

		#region Private Methods

		#endregion
	}
}


