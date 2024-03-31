using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class WorldSprite : WorldObject
	{
		#region Events

		public Func<string, bool> PlaySFX;
		public Action<string, Vector3> CreatePFX;

		#endregion

		#region Public Members

		public int Depth
		{
			get { return _depth; }
			set { _depth = value; }
		}

		#endregion

		#region Private Members

		/// <summary> Get the BoxCollider2D component of this sprite. </summary>
		private BoxCollider2D _collider;

		/// <summary> Get the SpriteRenderer component of this sprite. </summary>
		[SerializeField] private SpriteRenderer _renderer;

		/// <summary> If this sprite has obstacle data. </summary>
		private bool _hasObstacleData = false;

		/// <summary> The sprites core animation data. </summary>
		[SerializeField] private SpriteAnimation _spriteAnimation;

		/// <summary> The sprites obstacle data. </summary>
		private Obstacle _obstacle;

		[SerializeField] private int _currDefaultAnimationFrame = 0;
		[SerializeField] private float _currDefaultAnimationTime = 0f;
		[SerializeField] private bool _animationDestroying = false;
		[SerializeField] private int _currDestroyAnimationFrame = 0;
		[SerializeField] private float _currDestroyAnimationTime = 0f;
		[SerializeField] private bool _animationHit = false;
		[SerializeField] private int _currHitAnimationFrame = 0;
		[SerializeField] private float _currHitAnimationTime = 0f;

		[SerializeField] private int _depth = 0;

		[SerializeField] private bool _hasCollider = false;

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();
			InitializeEvents();
			_renderer = GetComponent<SpriteRenderer>();
		}

		#endregion

		#region Public Methods

		/// <summary> Reset the sprite to default. </summary>
		public void ResetSprite()
		{
			DisableCollider();
			_animationDestroying = false;
		}

		/// <summary> Set the WorldSprites core animation data. </summary>
		/// <param name="spriteAnimation"> The sprite animation to set to. </param>
		public void SetSpriteAnimation(SpriteAnimation spriteAnimation)
		{
			_spriteAnimation = spriteAnimation;
		}

		/// <summary> Set the SpriteRenderers Sprite. </summary>
		/// <param name="s"> The sprite to set to. </param>
		public void SetSprite(Sprite s)
		{
			_renderer.sprite = s;
		}

		/// <summary> Set the SpriteRenderers sorting order. </summary>
		/// <param name="id"> The sorting order id. </param>
		public void SetSortingOrder(int id)
		{
			_renderer.sortingOrder = id;
		}

		/// <summary> Set the SpriteRenderers sorting layer. </summary>
		/// <param name="name"> The name of the sorting layer. </param>
		public void SetSortingLayer(string name)
		{
			_renderer.sortingLayerName = name;
		}

		/// <summary> Set the sprites obstacle data. </summary>
		/// <param name="obstacle"> The new obstacle data. </param>
		public void SetObstacleData(Obstacle obstacle)
		{
			_obstacle = obstacle;
			name = $"Obstacle[{obstacle.Name}][{UID}]";

			// enable the collider as this is an obstacle now.
			EnableCollider(obstacle.ColliderData.Size, obstacle.ColliderData.Offset);
			_hasObstacleData = true;
		}

		/// <summary> Does the sprite have obstacle data? </summary>
		/// <returns> True if sprite has obstacle data. </returns>
		public bool HasObstacleData()
		{
			return _hasObstacleData;
		}

		/// <summary> Get the obstacle data if it exists. </summary>
		/// <param name="obstacle"> The obstacle data that will be outed if it exists. </param>
		/// <returns> True if the sprite has obstacle data. </returns>
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

		/// <summary> Enable the sprite collider given a size and offset. </summary>
		/// <param name="size"> The new size of the collider. </param>
		/// <param name="offset"> The new offset of the collider. </param>
		public void EnableCollider(Vector2 size, Vector2 offset)
		{
			if(_hasCollider)
			{
				return;
			}

			if (_collider == null)
			{
				_collider = gameObject.AddComponent<BoxCollider2D>();
			}

			_depth = 1;

			_collider.size = size;
			_collider.offset = offset;
			_collider.isTrigger = true;
			_collider.enabled = true;
			_hasCollider = true;
		}

		/// <summary> Disables the sprite collider. </summary>
		public void DisableCollider()
		{
			if (_collider)
			{
				//_collider.size = Vector2.zero;
				//_collider.offset = Vector2.zero;
				//_collider.enabled = false;
				_hasCollider = false;
				Destroy(_collider);
			}
		}

		/// <summary> Disables the obstacle data. </summary>
		public void DisableObstacleData()
		{
			_hasObstacleData = false;
		}

		/// <summary> Behavior for colliding with Player. </summary>
		public void CollidePlayer()
		{
			if (!_hasObstacleData)
			{
				return;
			}

			if(_animationDestroying)
			{
				return;
			}

			if(_obstacle.SFXOnCollision)
			{
				PlaySFX(_obstacle.CollisionSFXID);
			}

			if(_obstacle.PFXOnCollision)
			{
				CreatePFX(_obstacle.CollisionPFXID, transform.position);
			}

			if(_obstacle.DestroyOnCollision)
			{
				if(_spriteAnimation.DestroyedAnimation.Count > 0)
				{
					_currDestroyAnimationFrame = 0;
					_renderer.sprite = _spriteAnimation.DestroyedAnimation[0].Sprite;
					_animationDestroying = true;
					return;
				}

				DestroySelf();
				return;
			}

			if (_spriteAnimation.HitAnimation.Count > 0 && !_animationHit)
			{
				_currHitAnimationFrame = 0;
				_renderer.sprite = _spriteAnimation.HitAnimation[0].Sprite;
				_animationHit = true;
				return;
			}

			// invoke on collided event for this object
			// OnObjectCollide.Invoke(UID);
		}

		/// <summary>
		/// Behavior for colliding with player in center.
		/// </summary>
		public void CollidePlayerCenter()
		{
			// if no obstacle data return
			if (!_hasObstacleData)
			{
				return;
			}

			if(_animationDestroying)
			{
				return;
			}

			// invoke sfx event if obstacle has
			if (_obstacle.SFXOnCenterCollision)
			{
				PlaySFX(_obstacle.CenterCollisionSFXID);
			}

			// invoke pfx event if obstacle has
			if (_obstacle.PFXOnCenterCollision) 
			{
				CreatePFX(_obstacle.CenterCollisionPFXID, transform.position);
			}

			// now collide regularly
			CollidePlayer();
		}

		public override void UpdateObjectAnimation()
		{
			if (_animationDestroying)
			{
				UpdateAnimationFrame(ref _currDestroyAnimationTime, 
					ref _currDestroyAnimationFrame, 
					_spriteAnimation.DestroyedAnimation);
				if (_currDestroyAnimationFrame >= _spriteAnimation.DestroyedAnimation.Count)
				{
					DestroySelf();
					_animationDestroying = false;
					return;
				}

				return;
			}

			if (_animationHit)
			{
				UpdateAnimationFrame(ref _currHitAnimationTime,
					ref _currHitAnimationFrame,
					_spriteAnimation.HitAnimation);

				if (_currHitAnimationFrame >= _spriteAnimation.HitAnimation.Count)
				{
					_animationHit = false;
					return;
				}

				return;
			}

			if(_spriteAnimation.DefaultAnimation.Count > 1)
			{
				UpdateAnimationFrame(ref _currDefaultAnimationTime, ref _currDefaultAnimationFrame, _spriteAnimation.DefaultAnimation);
				if (_currDefaultAnimationFrame >= _spriteAnimation.DefaultAnimation.Count)
				{
					_currDefaultAnimationFrame = 0;
				}
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Initializes the core events.
		/// </summary>
		private void InitializeEvents()
		{
			// OnObjectCollide = (id) => { };
		}

		private void DestroySelf()
		{
			if (HasParentChunk && !IsParentChunk)
			{
				ParentChunk.RemoveWorldSprite(this);
			}

			SelfUpdate -= UpdateObjectAnimation;
			TriggerDestroy();
		}

		private void UpdateAnimationFrame(ref float timer, ref int currFrame, List<SpriteAnimationFrame> frames)
		{
			if (currFrame < frames.Count)
			{
				_renderer.sprite = frames[currFrame].Sprite;
			}

			timer += Time.deltaTime;
			if (timer >= frames[currFrame].FrameTime)
			{
				currFrame += 1;
				timer = 0f;
			}
		}

		#endregion
	}
}

