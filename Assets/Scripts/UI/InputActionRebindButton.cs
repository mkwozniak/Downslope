using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Text = TMPro.TextMeshProUGUI;

namespace Wozware.Downslope
{
	[RequireComponent(typeof(Button))]
	public class InputActionRebindButton : MonoBehaviour
	{
		#region Events

		#endregion

		#region Public Members

		public Text ButtonText;

		#endregion

		#region Private Members

		[SerializeField] private RebindableControlTypes _rebindableType;
		[SerializeField] private int bindingIndex;

		private InputAction _action;

		private Button _rebindButton;

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods


		private void SetButtonText()
		{
			ButtonText.text = _action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
		}

		private void RebindAction()
		{
			if (DownslopeInput.REBINDING)
			{
				SetButtonText();
				return;
			}


			ButtonText.text = "...";

			_action.Disable();

			DownslopeInput.SuccessfulRebinding += OnSuccessfulRebinding;
			DownslopeInput.FailedRebinding += OnFailedRebinding;

			bool isGamepad = _action.bindings[bindingIndex].path.Contains("Gamepad");

			if (isGamepad)
				DownslopeInput.RemapGamepadAction(_action, bindingIndex);
			else
				DownslopeInput.RemapKeyboardAction(_action, bindingIndex);

			DownslopeInput.REBINDING = true;
		}

		private void OnSuccessfulRebinding(InputAction action)
		{
			DownslopeInput.SuccessfulRebinding -= OnSuccessfulRebinding;
			DownslopeInput.FailedRebinding -= OnFailedRebinding;

			SetButtonText();
			DownslopeInput.REBINDING = false;
			_action.Enable();
		}
		private void OnFailedRebinding(InputAction action)
		{
			DownslopeInput.SuccessfulRebinding -= OnSuccessfulRebinding;
			DownslopeInput.FailedRebinding -= OnFailedRebinding;

			SetButtonText();
			DownslopeInput.REBINDING = false;
			_action.Enable();
		}


		#endregion

		#region Unity Methods

		private void Awake()
		{
			_rebindButton = GetComponent<Button>();
			_rebindButton.onClick.AddListener(RebindAction);
		}

		private void OnEnable()
		{
			
		}

		private void OnDestroy()
		{
			_rebindButton.onClick.RemoveAllListeners();
		}

		private void Start()
		{
			_action = DownslopeInput.GetRebindableAction(_rebindableType);
			SetButtonText();
		}

		private void Update()
		{

		}

		#endregion
	}

}
