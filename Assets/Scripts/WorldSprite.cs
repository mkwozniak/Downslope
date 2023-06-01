using System.Collections.Generic;
using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using UnityEngine.U2D;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class WorldSprite : MonoBehaviour
	{
		#region Events

		public ObjectColliding OnObjectCollide;
		public FXCreating CreateSFX;
		public FXCreating CreatePFX;

		#endregion

		#region Public Members

		public int UID;
		public int LowerLayerID = 1;
		public bool SelfGenerateUID = false;
		public bool HasCollider;
		public bool AirCollidable = false;
		public CollisionTypes CollisionType;

		#endregion

		#region Private Members

		private BoxCollider2D _collider;
		[SerializeField] private Animator _animator;
		[SerializeField] private SpriteRenderer _renderer;
		private bool _hasObstacleData = false;
		private Obstacle _obstacle;

		#endregion

		#region Public Methods

		public void ResetSprite()
		{
			DisableAnimator();
			DisableCollider();
		}

		public void SetSprite(Sprite s)
		{
			_renderer.sprite = s;
		}

		public void SetParent(Transform parent)
		{
			transform.SetParent(parent);
		}

		public void SetSortingOrder(int id)
		{
			_renderer.sortingOrder = id;
		}

		public void SetSortingLayer(string name)
		{
			_renderer.sortingLayerName = name;
		}

		public void SetObstacleData(Obstacle obstacle)
		{
			_obstacle = obstacle;
			SetSortingLayer(obstacle.SortingLayer);
			EnableCollider(obstacle.ColliderData.Size, obstacle.ColliderData.Offset);
			name = $"Obstacle[{obstacle.Name}][{UID}]";
			_hasObstacleData = true;
		}

		public bool HasObstacleData()
		{
			return _hasObstacleData;
		}

		public bool TryGetObstacleData(out Obstacle obstacle)
		{
			if(!_hasObstacleData)
			{
				Debug.LogError($"WorldSprite {transform.name}[{UID}] GetObstacleData does not have any obstacle data. Returning empty data.");
				obstacle = new Obstacle();
				return false;
			}

			obstacle = _obstacle;
			return true;
		}

		public void EnableAnimator(RuntimeAnimatorController controller)
		{
			if (!_animator)
				return;
			_animator.enabled = true;
			_animator.runtimeAnimatorController = controller;
			_animator.ResetTrigger("Hit");
		}

		public void EnableCollider(Vector2 size, Vector2 offset)
		{
			if (_collider == null)
			{
				_collider = gameObject.AddComponent<BoxCollider2D>();
			}

			_collider.size = size;
			_collider.offset = offset;
			_collider.isTrigger = true;
			_collider.enabled = true;

			HasCollider = true;
		}

		public void DisableCollider()
		{
			if (_collider)
			{
				Bounds b = _collider.bounds;
				b.size = Vector2.one;
				_collider.offset = Vector2.zero;
				_collider.enabled = false;
			}

			AirCollidable = false;
			HasCollider = false;
		}

		public void DisableObstacleData()
		{
			_hasObstacleData = false;
		}

		public void CollidePlayer()
		{
			if (!_hasObstacleData)
			{
				return;
			}

			if(_obstacle.SFXOnCollision)
			{
				CreateSFX.Invoke(_obstacle.CollisionSFXID, transform.position);
			}

			if(_obstacle.PFXOnCollision)
			{
				CreatePFX.Invoke(_obstacle.CollisionPFXID, transform.position);
			}

			OnObjectCollide.Invoke(UID);

			if (_animator)
			{
				if (_animator.enabled)
				{
					_animator.SetTrigger("Hit");
				}
			}
		}

		public void CollidePlayerCenter()
		{
			if (!_hasObstacleData)
			{
				return;
			}

			if (_obstacle.SFXOnCenterCollision)
			{
				CreateSFX.Invoke(_obstacle.CenterCollisionSFXID, transform.position);
			}

			if(_obstacle.PFXOnCenterCollision) 
			{
				CreatePFX.Invoke(_obstacle.CenterCollisionPFXID, transform.position);
			}

			CollidePlayer();
		}

		#endregion

		#region Private Methods

		private void InitializeEvents()
		{
			OnObjectCollide = (id) => { };
		}

		private void DisableAnimator()
		{
			if (!_animator)
				return;

			if (_animator.enabled)
			{
				_animator.ResetTrigger("Hit");
				_animator.enabled = false;
			}
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			InitializeEvents();
			_renderer = GetComponent<SpriteRenderer>();
			_collider = GetComponent<BoxCollider2D>();

			if (_animator)
			{
				_animator.enabled = false;
			}
		}

		private void Start() { }

		private void Update() { }

		#endregion
	}
}

