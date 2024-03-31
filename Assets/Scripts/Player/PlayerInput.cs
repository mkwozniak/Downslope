using System.Collections.Generic;
using UnityEngine;
using Wozware.Downslope;

namespace Wozware.Downslope
{
	public sealed partial class PlayerControl : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		public Dictionary<RebindableControlTypes, bool> InputTriggers = new();

		#endregion

		#region Private Members

		#endregion

		#region Public Methods

		public void InitializeInputEvents()
		{
			DownslopeInput.SubscribeInputPerformed(DownslopeInput.PLAYER_SENDIT, EnableSendItEffects);
			DownslopeInput.SubscribeInputCancelled(DownslopeInput.PLAYER_SENDIT, DisableSendItEffects);
		}

		#endregion

		#region Private Methods

		private void InputPerformCarveLeft()
		{
			_inputHorizontal[0] = true;
			InputTriggers[RebindableControlTypes.Player_Carve_Left] = true;
		}

		private void InputCancelCarveLeft()
		{
			_inputHorizontal[0] = false;
			InputTriggers[RebindableControlTypes.Player_Carve_Left] = false;
		}

		private void InputPerformCarveRight()
		{
			_inputHorizontal[1] = true;
		}

		private void CancelInputCarveRight()
		{
			_inputHorizontal[1] = false;
		}

		private void PerformInputBrake()
		{
			_inputHorizontal[1] = true;
		}

		private void CancelInputBreak()
		{
			_inputHorizontal[1] = false;
		}

		#endregion
	}
}

