using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Downslope
{
	
	public class WorldSpritePooler : MonoBehaviour
	{
		public Transform Parent;

		public GameObject Prefab;

		public int MinUpkeep;

		public int MaxInactive;

		private Queue<WorldSprite> _pool = new Queue<WorldSprite>();
		private bool _trimming = false;

		public bool Valid()
		{
			if (_pool.Count == 0)
				return false;

			return true;
		}

		public WorldSprite GetFromPool()
		{
			if (_pool.Count == 0)
				AddToPool();
			return _pool.Dequeue();
		}

		public void ReturnToPool(WorldSprite sprite)
		{
			sprite.SetParent(Parent);
			sprite.gameObject.SetActive(false);
			sprite.gameObject.name = $"DormantSprite[{sprite.UID}]";
			sprite.ResetSprite();
			_pool.Enqueue(sprite);
		}

		private void GenerateInitialPool()
		{
			for (int i = 0; i < MinUpkeep; i++)
			{
				AddToPool();
			}
		}

		private void Awake()
		{
			GenerateInitialPool();
		}

		private void Start()
		{

		}

		private void Update()
		{
			if (_pool.Count > MaxInactive && !_trimming)
			{
				_trimming = true;
				DestroyExcess();
			}
		}

		private void DestroyExcess()
		{
			for (int i = 0; i < MaxInactive - _pool.Count; i++)
			{
				Destroy(_pool.Dequeue());
			}
			_trimming = false;
		}

		private void AddToPool()
		{
			GameObject newElement = Instantiate(Prefab, Parent);
			newElement.SetActive(false);
			WorldSprite sprite = newElement.GetComponent<WorldSprite>();
			sprite.UID = newElement.GetHashCode() + _pool.Count;
			newElement.name = $"ActiveSprite[{sprite.UID}]";
			_pool.Enqueue(sprite);
		}
	}
}

