using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using UnityEngine.InputSystem;
using Action = System.Action;

namespace Wozware.Downslope
{
	public static class DownslopeInput
	{
		#region Events

		public static Action<InputAction> SuccessfulRebinding;
		public static Action<InputAction> FailedRebinding;

		#endregion

		#region Public Members

		public static string GAME_ENTER;
		public static string GAME_ESCAPE;
		public static string GAME_SELECT;

		public static string PLAYER_CARVE_LEFT;
		public static string PLAYER_CARVE_RIGHT;
		public static string PLAYER_BRAKE;
		public static string PLAYER_SENDIT;
		public static string PLAYER_CURSOR;

		public static string LEVELCREATOR_SCROLL;
		public static string LEVELCREATOR_PLACE;
		public static string LEVELCREATOR_SHIFT;
		public static string LEVELCREATOR_CTRL;

		public static UnityEngine.Vector2 CURSOR_POSITION;
		public static float AXIS_LEVELCREATOR_SCROLL;

		public static Dictionary<string, bool> INPUTS = new();
		public static Dictionary<string, InputAction> ACTIONS = new();
		public static Dictionary<string, string> CONTROL_OVERRIDES = new();
		public static Dictionary<RebindableControlTypes, string> REBINDABLES = new();

		public static bool REBINDING = false;

		public static void EnablePlayerInputActionMap(bool val)
		{
			if(val)
			{
				_CONTROLS.Player.Enable();
				return;
			}

			_CONTROLS.Player.Disable();
		}

		public static void EnableLevelCreatorInputActionMap(bool val)
		{
			if (val)
			{
				_CONTROLS.LevelCreator.Enable();
				return;
			}

			_CONTROLS.LevelCreator.Disable();
		}

		public static void EnableGameInputActionMap(bool val)
		{
			if (val)
			{
				_CONTROLS.Game.Enable();
				return;
			}

			_CONTROLS.Game.Disable();
		}

		public static InputAction GetRebindableAction(RebindableControlTypes rebindableType)
		{
			return ACTIONS[REBINDABLES[rebindableType]];
		}

		public static void ApplyBindingOverride(Guid guid, int index, string path)
		{
			_CONTROLS.FindAction(guid.ToString()).ApplyBindingOverride(index, path);
		}

		public static void RemapKeyboardAction(InputAction actionToRebind, int targetBinding)
		{
			var rebindOperation = actionToRebind.PerformInteractiveRebinding(targetBinding)
				.WithBindingGroup(_CONTROLS.KeyboardScheme.bindingGroup)
				.WithCancelingThrough(ACTIONS[REBINDABLES[RebindableControlTypes.Game_Escape]].bindings[0].effectivePath)
				.OnCancel(operation => FailedRebinding?.Invoke(null))
				.OnComplete(operation => {
					operation.Dispose();
					AddOverrideToDictionary(actionToRebind.id, actionToRebind.bindings[targetBinding].effectivePath, targetBinding);
					DownslopeFiles.SaveControlOverrides(CONTROL_OVERRIDES);
					SuccessfulRebinding?.Invoke(actionToRebind);
				})
				.Start();
		}

		public static void RemapGamepadAction(InputAction actionToRebind, int targetBinding)
		{
			var rebindOperation = actionToRebind.PerformInteractiveRebinding(targetBinding)
				.WithControlsHavingToMatchPath("<Gamepad>")
				.WithBindingGroup("Gamepad")
				.WithCancelingThrough("<Keyboard>/escape")
				.OnCancel(operation => FailedRebinding?.Invoke(null))
				.OnComplete(operation => {
					operation.Dispose();
					AddOverrideToDictionary(actionToRebind.id, actionToRebind.bindings[targetBinding].effectivePath, targetBinding);
					DownslopeFiles.SaveControlOverrides(CONTROL_OVERRIDES);
					SuccessfulRebinding?.Invoke(actionToRebind);
				})
				.Start();
		}


		#endregion

		#region Private Members

		private static DownslopeControls _CONTROLS;
		private static Dictionary<string, Action> _PERFORMED = new Dictionary<string, Action>();
		private static Dictionary<string, Action> _CANCELLED = new Dictionary<string, Action>();

		#endregion

		#region Public Methods

		/// <summary>
		/// Initializes the DownslopeControls class and binds its actions.
		/// </summary>
		public static void InitializeControls()
		{
			_CONTROLS = new DownslopeControls();

			BindAction(_CONTROLS.Game.Enter, ref GAME_ENTER);
			BindAction(_CONTROLS.Game.Escape, ref GAME_ESCAPE);
			BindAction(_CONTROLS.Game.Select, ref GAME_SELECT);

			BindAction(_CONTROLS.Player.CarveLeft, ref PLAYER_CARVE_LEFT);
			BindAction(_CONTROLS.Player.CarveRight, ref PLAYER_CARVE_RIGHT);
			BindAction(_CONTROLS.Player.Brake, ref PLAYER_BRAKE);
			BindAction(_CONTROLS.Player.SendIt, ref PLAYER_SENDIT);

			BindAction(_CONTROLS.LevelCreator.Scroll, ref LEVELCREATOR_SCROLL);
			BindAction(_CONTROLS.LevelCreator.Place, ref LEVELCREATOR_PLACE);
			BindAction(_CONTROLS.LevelCreator.ShiftMode, ref LEVELCREATOR_SHIFT);
			BindAction(_CONTROLS.LevelCreator.ControlMode, ref LEVELCREATOR_CTRL);

			_CONTROLS.Global.CursorPosition.Enable();
			PLAYER_CURSOR = _CONTROLS.Global.CursorPosition.name;
			ACTIONS[PLAYER_CURSOR] = _CONTROLS.Global.CursorPosition;

			REBINDABLES[RebindableControlTypes.Game_Enter] = GAME_ENTER;
			REBINDABLES[RebindableControlTypes.Game_Escape] = GAME_ESCAPE;
			REBINDABLES[RebindableControlTypes.Player_Brake] = PLAYER_BRAKE;
			REBINDABLES[RebindableControlTypes.Player_Carve_Left] = PLAYER_CARVE_LEFT;
			REBINDABLES[RebindableControlTypes.Player_Carve_Right] = PLAYER_CARVE_RIGHT;
			REBINDABLES[RebindableControlTypes.Player_SendIt] = PLAYER_SENDIT;
			REBINDABLES[RebindableControlTypes.LC_Place] = LEVELCREATOR_PLACE;
			REBINDABLES[RebindableControlTypes.LC_Scroll] = LEVELCREATOR_SCROLL;
			REBINDABLES[RebindableControlTypes.LC_Shift] = LEVELCREATOR_SHIFT;
			REBINDABLES[RebindableControlTypes.LC_Ctrl] = LEVELCREATOR_CTRL;
		}

		/// <summary>
		/// Subscribe to performed input event with an action.
		/// </summary>
		/// <param name="input"> The name of the input. </param>
		/// <param name="callback"> The callback to subscribe. </param>
		public static void SubscribeInputPerformed(string input, Action callback)
		{
			if (!_PERFORMED.ContainsKey(input))
				return;
			_PERFORMED[input] += callback;
		}

		/// <summary>
		/// Subscribe to cancelled input event with an action.
		/// </summary>
		/// <param name="input"> The name of the input. </param>
		/// <param name="callback"> The callback to subscribe. </param>
		public static void SubscribeInputCancelled(string input, Action callback)
		{
			if (!_CANCELLED.ContainsKey(input))
				return;
			_CANCELLED[input] += callback;
		}

		/// <summary>
		/// Usubscribe to performed input event with an action.
		/// </summary>
		/// <param name="input"> The name of the input. </param>
		/// <param name="callback"> The callback to unsubscribe. </param>
		public static void UnsubscribeInputPerformed(string input, Action callback)
		{
			if (!_PERFORMED.ContainsKey(input))
				return;
			_PERFORMED[input] -= callback;
		}

		/// <summary>
		/// Unsubscribe from cancelled input event with an action.
		/// </summary>
		/// <param name="input"> The name of the input.</param>
		/// <param name="callback"> The callback to unsubscribe. </param>
		public static void UnsubscribeInputCancelled(string input, Action callback)
		{
			if (!_CANCELLED.ContainsKey(input))
				return;
			_CANCELLED[input] -= callback;
		}

		public static void UpdateInputValues()
		{
			CURSOR_POSITION = ACTIONS[PLAYER_CURSOR].ReadValue<UnityEngine.Vector2>();
			AXIS_LEVELCREATOR_SCROLL = ACTIONS[LEVELCREATOR_SCROLL].ReadValue<float>();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Binds an input action by enabling it and adding its default callbacks.
		/// </summary>
		/// <param name="action"> The Unity InputAction to bind. </param>
		private static void BindAction(InputAction action, ref string name)
		{
			name = action.name;
			ACTIONS[name] = action;
			ACTIONS[name].Enable();
			ACTIONS[name].performed += OnInputPerformed;
			ACTIONS[name].canceled += OnInputCanceled;
			_PERFORMED.Add(name, () => { });
			_CANCELLED.Add(name, () => { });
			INPUTS[name] = false;
		}

		/// <summary>
		/// Callback for when an input is performed.
		/// </summary>
		/// <param name="action"></param>
		private static void OnInputPerformed(InputAction.CallbackContext action)
		{
			string name = action.action.name;
			INPUTS[name] = true;

			if (_PERFORMED.ContainsKey(name))
			{
				_PERFORMED[name].Invoke();
			}
		}

		/// <summary>
		/// Callback for when an input is cancelled.
		/// </summary>
		/// <param name="action"></param>
		private static void OnInputCanceled(InputAction.CallbackContext action)
		{
			string name = action.action.name;
			INPUTS[name] = false;

			if (_CANCELLED.ContainsKey(name))
			{
				_CANCELLED[name].Invoke();
			}
		}

		private static void AddOverrideToDictionary(Guid actionId, string path, int bindingIndex)
		{
			string key = string.Format("{0} : {1}", actionId.ToString(), bindingIndex);

			if (CONTROL_OVERRIDES.ContainsKey(key))
			{
				CONTROL_OVERRIDES[key] = path;
			}
			else
			{
				CONTROL_OVERRIDES.Add(key, path);
			}
		}


		#endregion
	}
}


