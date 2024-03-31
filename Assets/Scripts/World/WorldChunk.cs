using System;
using System.Collections.Generic;

namespace Wozware.Downslope
{
	public sealed class WorldChunk : WorldObject
	{
		#region Events

		#endregion

		#region Public Members

		public Dictionary<int, (bool hasObstacle, WorldSprite sprite)> ObstacleFlags = new();
		public List<WorldSprite> _worldObjects = new();
		public float XOffset;
		public float OffsetCorrectionHeight;

		#endregion

		#region Private Members

		#endregion

		#region Public Methods

		public void AddWorldSprite(WorldSprite obj)
		{
			_worldObjects.Add(obj);
		}

		public void RemoveWorldSprite(WorldSprite obj)
		{
			_worldObjects.Remove(obj);
		}

		public void DestroyChunkedObjects()
		{
			ReadOnlySpan<WorldObject> chunks = new ReadOnlySpan<WorldObject>(_worldObjects.ToArray());

			//Debug.Log($"Chunk {UID} destroyed all {_worldObjects.Count} chunked objects within.");

			for (int i = 0; i < _worldObjects.Count; i++)
			{
				chunks[i].TriggerDestroy();
			}
		}

		public override void TriggerDestroy()
		{
			DestroyChunkedObjects();
			_worldObjects.Clear();
			base.TriggerDestroy();
		}

		protected override void Update()
		{
			base.Update();
			if(transform.position.x > OffsetCorrectionHeight)
			{
				if(OnSelfReachedOffsetThreshold != null)
				{
					OnSelfReachedOffsetThreshold(this, XOffset);
				}
			}
		}

		#endregion
	}
}

