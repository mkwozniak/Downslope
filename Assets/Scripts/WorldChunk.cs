using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	public sealed class WorldChunk : MonoBehaviour
	{
		public WorldChunkAction OnDestroy;
		public WorldChunkAction OnReachHeightThreshold;

		public int Thickness;
		public List<Transform> CenterSprites = new List<Transform>();
		public List<string> PossibleChunks = new List<string>();
		public List<string> PossibleExpandChunks = new List<string>();
		public List<string> PossibleContractChunks = new List<string>();
		public SpriteRenderer LeftEdge;
		public SpriteRenderer RightEdge;
		public int UID;

		private WorldMovementProps _movement;
		private bool _destroyed;
		private bool _heightThresholdActive;

		private Vector3 _velocity;

		private void Awake()
		{

		}

		private void Start()
		{

		}

		public void SetMovement(WorldMovementProps props)
		{
			_movement = props;
		}

		public void ReachHeightThresholdSubscribe(WorldChunkAction callback)
		{
			_heightThresholdActive = true;
			OnReachHeightThreshold += callback;
		}

		public void ReachHeightThresholdUnsubscribe(WorldChunkAction callback)
		{
			_heightThresholdActive = false;
			OnReachHeightThreshold -= callback;
		}

		public void UpdateChunkMovement(float speed)
		{
			if (_destroyed)
				return;

			_velocity.x = _movement.Direction.x * speed;
			_velocity.y = _movement.Direction.y * speed;

			transform.position += _velocity;
		}

		public void UpdateChunk()
		{
			if (transform.position.y > _movement.MaxY && !_destroyed)
			{
				OnDestroy.Invoke(this);
				_destroyed = true;
			}

			if (_heightThresholdActive && transform.position.y > _movement.HeightThresholdY)
			{
				OnReachHeightThreshold.Invoke(this);
			}
		}
	}
}


