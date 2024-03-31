using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.Poolers
{
	/// <summary>
	/// WozPooler is a very basic non Monobehavior object pooler.
	/// It will sit idle until you request an object from its pool. If the pool is empty, an object is Instantiated.
	/// If the pool has one, it will dequeue that object and out it. Once finished with an object, call ReturnToPool.
	/// Make sure to subscribe to OnAddToPool and OnReturnToPool events to decide what to do with your pooled objects. 
	/// </summary>
	/// <typeparam name="T"> The type of object to pool. </typeparam>
	[System.Serializable]
	public class WozPooler<T>
	{
		#region Events

		public Action<T> OnAddToPool;
		public Action<T> OnReturnToPool;
		public Action<T> OnDestroyExcess;

		#endregion

		#region Public Members & Fields

		public GameObject Prefab;
		public Transform PoolParent;
		public int MinUpkeep;
		public int MaxInactive;
		public bool PrePool = false;

		public int Count
		{
			get { return _pool.Count; }
		}

		public bool Valid
		{
			get { return _valid; }
		}

		#endregion

		#region Private Members

		private Queue<T> _pool;
		private bool _valid = false;
		private bool _trimming = false;

		#endregion

		#region Public Methods

		/// <summary>
		/// Initializes the pool queue and checks if the prefab is of valid type.
		/// </summary>
		public void Initialize()
		{
			if(Prefab.GetComponent<T>() == null)
			{
				Debug.LogError($"WozPooler fatal initialization error. Prefab Object does not contain desired Component: {typeof(T)}");
				_valid = false;
				return;
			}

			_pool = new Queue<T>();
			_valid = true;

			if(PrePool)
			{
				for(int i = 0; i < MinUpkeep; i++)
				{
					AddToPool();
				}
			}
		}

		/// <summary>
		/// Dequeues an object from the pool. If the count is 0, first adds a new object.
		/// </summary>
		/// <returns></returns>
		public bool GetFromPool(out T obj)
		{
			// invalid pool will return an empty copy of the prefab
			if (!_valid)
			{
				Debug.LogError($"WozPooler cannot GetFromPool on an invalid pool. May return a null reference. {typeof(T)}");
				obj = UnityEngine.Object.Instantiate(Prefab).GetComponent<T>();
				return false;
			}

			// if count 0, add new
			if (_pool.Count == 0)
				AddToPool();

			// out the dequeued object
			obj = _pool.Dequeue();
			return true;
		}

		/// <summary>
		/// Enqueues the object back to pool and invokes OnReturnToPool event.
		/// </summary>
		/// <param name="obj"></param>
		public void ReturnToPool(T obj)
		{
			if (!_valid)
				return;

			// enqueue the obj to pool and call event if not null
			_pool.Enqueue(obj);
			if (OnReturnToPool != null)
			{
				OnReturnToPool.Invoke(obj);
			}
		}

		/// <summary>
		/// Checks if the pool needs to trim excess objects.
		/// </summary>
		public void CheckTrim()
		{
			if (!_valid)
				return;

			if(_pool.Count < MinUpkeep)
			{
				AddToPool();
			}

			// if count is greater than threshold then start trimming
			if (_pool.Count > MaxInactive && !_trimming)
			{
				_trimming = true;
				Debug.Log($"Pooler {this} Destroying Excess: {_pool.Count - MaxInactive}");
				DestroyExcess();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Enqueues a new inactive object to the pool and invokes the OnAddToPool event.
		/// </summary>
		private void AddToPool()
		{
			if (!_valid)
				return;

			// create a new element that will be pooled
			GameObject newElement = UnityEngine.Object.Instantiate(Prefab, PoolParent);

			// get the component from the obj
			T coreComponent = newElement.GetComponent<T>();
			_pool.Enqueue(coreComponent);

			// invoke the add to pool event if not null
			if (OnAddToPool != null)
			{
				OnAddToPool.Invoke(coreComponent);
			}
		}

		/// <summary>
		/// Invokes OnDestroyExcess event on excess objects and dequeues them.
		/// </summary>
		private void DestroyExcess()
		{
			// loop through excess values and dequeue for each one
			for (int i = 0; i < _pool.Count - MaxInactive; i++)
			{
				OnDestroyExcess.Invoke(_pool.Dequeue());
			}
			_trimming = false;
		}

		#endregion
	}
}
