using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(WorldObject))]
	[RequireComponent(typeof(PlayerControl))]
	public class PlayerAI : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		public WorldObject WorldObj
		{
			get
			{
				return _worldObject;
			}
		}

		public PlayerControl Controller
		{
			get
			{
				return _playerControl;
			}
		}

		#endregion

		#region Private Members

		private PlayerControl _playerControl;
		private WorldObject _worldObject;
		[SerializeField] private Vector3 _aiVelocity = Vector3.zero;

		#endregion

		#region Public Methods
		public void Initialize(WorldObjectProps props, WorldMovementProps movement)
		{
			_worldObject.Initialize(props, movement, true);
			_worldObject.SelfUpdate += UpdateAI;
			_worldObject.SelfFixedUpdate += UpdateAIMovement;
		}

		#endregion

		#region Private Methods

		private void UpdateAIMovement()
		{
			_worldObject.UpdateObjectMovement();
			_aiVelocity = new Vector3(0, _playerControl.CurrentForwardSpeed, 0);
			_worldObject.GetTransform().position -= _aiVelocity;
		}

		private void UpdateAI()
		{
			if (!_worldObject.Initialized)
			{
				return;
			}

			if (_playerControl.IsAI && _playerControl.State == PlayerStates.Moving)
			{
				_playerControl.CheckAIReachedGenThreshold.Invoke(transform.position.y, _playerControl.AIPlayerDistanceGeneration);
			}
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			_worldObject = GetComponent<WorldObject>();
			_playerControl = GetComponent<PlayerControl>();
		}

		private void Start()
		{

		}

		private void Update()
		{

		}

		#endregion
	}
}

