using System.Collections.Generic;
using UnityEngine;
using System;

namespace Wozware.Downslope
{
	public class WorldObject : MonoBehaviour
	{
		#region Events
		// events
		public event Action<WorldObject> OnDestroyTriggered;
		public event Action<WorldObject> OnReachHeightThreshold;

		// actions
		public static Func<float, bool> IsObjectAboveAI;
		public Action SelfUpdate;
		public Action SelfFixedUpdate;
		public Action<WorldObject, float> OnSelfReachedOffsetThreshold;

		#endregion

		#region Public Members

		/// <summary> The count ID of the object. </summary>
		public uint CountID;

		/// <summary> The unique ID of the object. </summary>
		public int UID;

		public bool HasParentChunk = false;
		public bool IsParentChunk = false;

		public WorldChunk ParentChunk;

		/// <summary> The object movement properties. </summary>
		public WorldMovementProps Movement
		{
			get
			{
				return _movement;
			}
		}

		/// <summary> If the object is initialized. </summary>
		public bool Initialized
		{
			get
			{
				return _initialized;
			}
		}

		/// <summary> If the object is destroyed. </summary>
		public bool Destroyed
		{
			get
			{
				return _destroyed;
			}
		}

		#endregion

		#region Private Members

		/// <summary> Current velocity of object </summary>
		private Vector3 _velocity;

		/// <summary> Current object properties </summary>
		private WorldObjectProps _props;

		/// <summary> Current object movement properties </summary>
		private WorldMovementProps _movement;

		/// <summary> If the object is initialized </summary>
		[SerializeField] private bool _initialized = false;

		/// <summary> If the object is destroyed </summary>
		[SerializeField] private bool _destroyed;

		/// <summary> The current speed of the object </summary>
		private float _currSpeed;

		/// <summary> If the object is current above an AI player </summary>
		private bool _aboveAI;

		/// <summary> If the object is going to trigger the OnReachHeightThreshold event. </summary>
		private bool _heightThresholdActive;

		/// <summary> If the object has valid movement data or not. </summary>
		[SerializeField] private bool _hasMovement = false;

		#endregion

		#region Unity Methods

		protected virtual void Awake()
		{

		}

		protected virtual void Update()
		{
			UpdateObjectAnimation();

			// check valid state
			if (HasInvalidUpdateState() || SelfUpdate == null)
			{
				return;
			}

			// invoke update event
			SelfUpdate();
		}

		private void FixedUpdate()
		{
			// check valid state
			if (HasInvalidUpdateState() || SelfFixedUpdate == null)
			{
				return;
			}

			// invoke fixed update event
			SelfFixedUpdate();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Initialize the world object to be active.
		/// </summary>
		/// <param name="props"> The base object properties. </param>
		/// <param name="movement"> The object movement properties. </param>
		/// <param name="disableDefaultBehavior"> Set to true disable the default Update and FixedUpdate behavior. </param>
		public virtual void Initialize(WorldObjectProps props, WorldMovementProps movement, bool disableDefaultBehavior = false)
		{
			_props = props;
			_movement = movement;
			_initialized = true;
			_destroyed = false;
			_hasMovement = true;

			if (!disableDefaultBehavior)
			{
				SelfUpdate = UpdateObject;
				SelfFixedUpdate = UpdateObjectMovement;
			}
		}

		/// <summary>
		/// Initialize the world object to be active.
		/// </summary>
		/// <param name="props"> The base object properties. </param>
		/// <param name="movement"> The object movement properties. </param>
		public void Initialize(WorldObjectProps props)
		{
			_props = props;
			_initialized = true;
			_destroyed = false;
			_hasMovement = false;
		}

		/// <summary>
		/// Set the object movement properties.
		/// </summary>
		/// <param name="movement"> The movement properties. </param>
		public void SetMovement(WorldMovementProps movement)
		{
			_movement = movement;
			_hasMovement = true;
		}

		/// <summary>
		/// Get the root GameObject of this object.
		/// </summary>
		/// <returns></returns>
		public GameObject GetObj()
		{
			return _props.Obj;
		}

		/// <summary>
		/// Get the root Transform of this object.
		/// </summary>
		/// <returns></returns>
		public Transform GetTransform()
		{
			return _props.Obj.transform;
		}

		/// <summary>
		/// Set the parent of the root object transform.
		/// </summary>
		/// <param name="transform"></param>
		public void SetParent(Transform transform)
		{
			_props.Obj.transform.SetParent(transform);
		}

		/// <summary>
		/// Set the objects current speed.
		/// </summary>
		/// <param name="speed"></param>
		public void SetObjectSpeed(float speed)
		{
			_currSpeed = speed;
		}

		/// <summary> Overridable object frame animation behavior. </summary>
		public virtual void UpdateObjectAnimation()
		{
			return;
		}

		/// <summary>
		/// Update the object frame movement.
		/// </summary>
		public void UpdateObjectMovement()
		{
			_velocity.x = _movement.Direction.x * _currSpeed;
			_velocity.y = _movement.Direction.y * _currSpeed;

			transform.position += _velocity;
		}

		/// <summary> Update the object frame behavior. </summary>
		public void UpdateObject()
		{
			if(_hasMovement)
			{
				UpdateMovement();
				return;
			}
		}

		/// <summary> Trigger the destroy event for the object. </summary>
		public virtual void TriggerDestroy()
		{
			// trigger destroy event
			if (OnDestroyTriggered != null)
			{
				OnDestroyTriggered.Invoke(this);
			}

			// set as destroyed
			SetAsDestroyed();
		}

		/// <summary> Set the object as considered to be destroyed. </summary>
		public void SetAsDestroyed()
		{
			// clear update events
			SelfUpdate = null;
			SelfFixedUpdate = null;
			ParentChunk = null;

			_initialized = false;
			HasParentChunk = false;
			IsParentChunk = false;
			_destroyed = true;
		}

		public void ReachHeightThresholdSubscribe(Action<WorldObject> callback)
		{
			_heightThresholdActive = true;
			OnReachHeightThreshold += callback;
		}

		public void ReachHeightThresholdUnsubscribe(Action<WorldObject> callback)
		{
			_heightThresholdActive = false;
			OnReachHeightThreshold -= callback;
		}

		#endregion

		#region Private Methods

		/// <summary> If the object has an invalid update state (destroyed, not initialized, paused) </summary>
		/// <returns> True if destroyed, not initialized, or game is paused. </returns>
		private bool HasInvalidUpdateState()
		{
			return (_destroyed || !_initialized || Game.IS_PAUSED);
		}

		private void UpdateMovement()
		{
			// check if above ai
			_aboveAI = IsObjectAboveAI(transform.position.y);

			// destroy only if above max Y, and not destroyed already and game destroy is enabled or above ai
			if (_props.Obj.transform.position.y > _movement.MaxY && !_destroyed && (Game.WORLD_DESTROY || _aboveAI))
			{
				TriggerDestroy();
			}

			if (_heightThresholdActive && transform.position.y > _movement.HeightThresholdY)
			{
				OnReachHeightThreshold(this);
			}
		}

		#endregion
	}
}

