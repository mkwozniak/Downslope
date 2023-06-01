using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wozware.Downslope
{
	[CustomEditor(typeof(PlayerControl))]
	public class PlayerControlEditor : Editor
	{
		private PlayerControl _targetInstance;
		private bool _showReadOnly;

		[ReadOnly] private SerializedProperty _inputStartMoving;

		private void OnEnable()
		{
			_targetInstance = (PlayerControl)target;
			_inputStartMoving = serializedObject.FindProperty("_inputStartMoving");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
		}
	}
}

