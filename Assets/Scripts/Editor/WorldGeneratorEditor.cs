using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wozware.Downslope
{
	[CustomEditor(typeof(WorldGenerator))]
	public class WorldGeneratorEditor : Editor
	{
		private WorldGenerator _targetInstance;

		private void OnEnable()
		{
			_targetInstance = (WorldGenerator)target;
		}

		public override void OnInspectorGUI()
		{
			//serializedObject.Update();
			base.OnInspectorGUI();
			//serializedObject.ApplyModifiedProperties();
		}
	}
}

