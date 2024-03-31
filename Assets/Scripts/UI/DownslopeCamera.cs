using System.Collections.Generic;
using UnityEngine;


namespace Wozware.Downslope
{
	public class DownslopeCamera : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		public float XOffsetShiftSpeed;
		public float XOffsetSnapThreshold;
		public float XOffsetDeltaThreshold;
		public int ShiftTriggerThreshold;
		public Transform Target;
		public float MaxDistanceFromTarget;
		public bool LerpMode;
		public bool FollowMode;

		#endregion

		#region Private Members

		private bool _shifting = false;
		private float _currX = 0f;
		private float _nextX = 0;

		private Vector3 _currPos;
		private Vector3 _targetPos;

		#endregion

		#region Public Methods

		public void ShiftToXOffset(float xOffset)
		{
			//Mathf.Abs(_currX - xOffset) < ShiftTriggerThreshold
			if (_currX == xOffset)
				return;

			_nextX = xOffset;
			_shifting = true;
		}

		#endregion

		#region Private Methods

		#endregion

		#region Unity Methods

		private void Awake()
		{

		}

		private void Start()
		{

		}

		private void Update()
		{
			_currPos = transform.position;
			_targetPos = Target.position;

			if (!FollowMode)
				return;

			float diff = Mathf.Abs(_currPos.x - _targetPos.x);

			if (diff > MaxDistanceFromTarget)
			{
				if (LerpMode)
				{
					_currX = Mathf.Lerp(_currX, _nextX, XOffsetShiftSpeed * DownslopeTime.DeltaTime);
				}
				else
				{
					if (_currPos.x < _targetPos.x)
						_currX += XOffsetShiftSpeed * DownslopeTime.DeltaTime;
					else if (_currPos.x > _targetPos.x)
						_currX -= XOffsetShiftSpeed * DownslopeTime.DeltaTime;
				}

				_currPos.x = _currX;
				transform.position = new Vector3(_currX, _currPos.y, _currPos.z);
			}
		}

		#endregion
	}
}

