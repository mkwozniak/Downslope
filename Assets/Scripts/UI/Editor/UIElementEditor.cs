using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace Wozware.Downslope
{
	[CustomEditor(typeof(UIElement))]
	public class UIElementEditor : Editor
	{
		private UIElement _targetInstance;

		private SerializedProperty hasButton;
		private SerializedProperty button;

		private SerializedProperty hasImage;
		private SerializedProperty sourceImage;

		private SerializedProperty hasHoverEvents;
		private SerializedProperty hoverEvents;

		private SerializedProperty hasHoverExitEvents;
		private SerializedProperty hoverExitEvents;

		private SerializedProperty hasHoverImageChange;
		private SerializedProperty hoverSprite;

		private SerializedProperty hasText;

		private SerializedProperty text;
		private SerializedProperty hasHoverTextChange;
		private SerializedProperty hoverMessage;

		private SerializedProperty hasHoverTextColorChange;
		private SerializedProperty hoverTextColor;

		private SerializedProperty hasHoverScaleChange;
		private SerializedProperty defaultScale;
		private SerializedProperty hoverScale;

		private SerializedProperty hasFloatingEffects;
		private SerializedProperty autoActivateFloatingEffects;
		private SerializedProperty floatingSpeed;
		private SerializedProperty floatingDirection;
		private SerializedProperty floatLoop;
		private SerializedProperty floatLoopMaxTime;
		private SerializedProperty floatLoopMessages;
		private SerializedProperty floatFadeTextOverTime;
		private SerializedProperty floatFadeFromTransparent;
		private SerializedProperty floatFadeSpeed;

		private void OnEnable()
		{
			_targetInstance = (UIElement)target;

			hasButton = serializedObject.FindProperty("HasButton");
			button = serializedObject.FindProperty("Btn");

			hasImage = serializedObject.FindProperty("HasImage");
			sourceImage = serializedObject.FindProperty("SourceImage");

			hasHoverEvents = serializedObject.FindProperty("HasHoverEvents");
			hoverEvents = serializedObject.FindProperty("OnHoverEvents");
			hasHoverExitEvents = serializedObject.FindProperty("HasHoverExitEvents");
			hoverExitEvents = serializedObject.FindProperty("OnHoverExitEvents");

			hasHoverImageChange = serializedObject.FindProperty("HasHoverImageChange");

			hoverSprite = serializedObject.FindProperty("HoverSprite");
			hasText = serializedObject.FindProperty("HasText");

			hasHoverTextChange = serializedObject.FindProperty("HasHoverTextChange");
			text = serializedObject.FindProperty("Txt");
			hoverMessage = serializedObject.FindProperty("HoverMessage");

			hasHoverTextColorChange = serializedObject.FindProperty("HasHoverTextColorChange");
			hoverTextColor = serializedObject.FindProperty("HoverTextColor");

			hasHoverScaleChange = serializedObject.FindProperty("HasHoverScaleChange");
			defaultScale = serializedObject.FindProperty("DefaultScale");
			hoverScale = serializedObject.FindProperty("HoverScale");

			hasFloatingEffects = serializedObject.FindProperty("HasFloatingEffects");
			autoActivateFloatingEffects = serializedObject.FindProperty("AutoActivateFloatingEffects");
			floatingSpeed = serializedObject.FindProperty("FloatingSpeed");
			floatingDirection = serializedObject.FindProperty("FloatingDirection");
			floatLoop = serializedObject.FindProperty("FloatLoop");
			floatLoopMaxTime = serializedObject.FindProperty("FloatLoopMaxTime");
			floatLoopMessages = serializedObject.FindProperty("FloatLoopMessages");
			floatFadeTextOverTime = serializedObject.FindProperty("FloatFadeTextOverTime");
			floatFadeFromTransparent = serializedObject.FindProperty("FloatFadeFromTransparent");
			floatFadeSpeed = serializedObject.FindProperty("FloatFadeSpeed");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			//base.OnInspectorGUI();

			GUILayout.Label("[Button Options]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasButton, new GUIContent("Has Button", "If this has a Unity Button."));
			if (_targetInstance.HasButton)
			{
				EditorGUILayout.PropertyField(button, new GUIContent("Source Button", "The reference to the Unity Button."));
			}

			GUILayout.Space(10f);

			GUILayout.Label("[Image Options]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasImage, new GUIContent("Has Image", "If this UI Element has an Image."));
			if(_targetInstance.HasImage)
			{
				EditorGUILayout.PropertyField(sourceImage, new GUIContent("Source Image", "The source image of this UIElement."));
				EditorGUILayout.PropertyField(hasHoverImageChange, new GUIContent("Hover Image Enabled", "If this UIElement should change the Button image on hovering."));
				if (_targetInstance.HasHoverImageChange)
				{
					EditorGUILayout.PropertyField(hoverSprite, new GUIContent("Hover Sprite", "The sprite to change to when hovering."));
				}
			}

			GUILayout.Space(10f);

			GUILayout.Label("[Text Options]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasText, new GUIContent("Has Text", "If this UI Element has a TextMeshPro Text component."));
			if (_targetInstance.HasText)
			{
				EditorGUILayout.PropertyField(text, new GUIContent("Source Text", "The TextMeshPro Text reference."));

				EditorGUILayout.PropertyField(hasHoverTextChange, new GUIContent("Hover Text Change", "If this UIElement should change the text message on hovering."));
				if (_targetInstance.HasHoverTextChange)
				{
					EditorGUILayout.PropertyField(hoverMessage, new GUIContent("Hover Message", "The message to set the text to when hovering."));
				}

				GUILayout.Space(5f);

				EditorGUILayout.PropertyField(hasHoverTextColorChange, new GUIContent("Hover Text Color Change", "If this UIElement should change the text color on hovering."));
				if (_targetInstance.HasHoverTextColorChange)
				{
					EditorGUILayout.PropertyField(hoverTextColor, new GUIContent("Hover Text Color", "The color of the text when hovering over."));
				}
			}

			GUILayout.Space(10f);

			GUILayout.Label("[Scale Options]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasHoverScaleChange, new GUIContent("Enabled", "If this UIElement should change its scale on hovering."));
			if (_targetInstance.HasHoverScaleChange)
			{
				EditorGUILayout.PropertyField(defaultScale, new GUIContent("Default Scale", "The default scale of the object."));
				EditorGUILayout.PropertyField(hoverScale, new GUIContent("Hover Scale", "The scale of the text when hovering over."));
			}

			GUILayout.Space(10f);

			GUILayout.Label("[Floating Effect Options]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasFloatingEffects, new GUIContent("Enabled", "If this UIElement has a floating effects enabled."));
			if (_targetInstance.HasFloatingEffects)
			{
				EditorGUILayout.PropertyField(autoActivateFloatingEffects, new GUIContent("Auto Activate", "If the floating effects should auto activate on Start."));
				EditorGUILayout.PropertyField(floatingSpeed, new GUIContent("Float Speed", "How fast the float effect applies."));
				EditorGUILayout.PropertyField(floatingDirection, new GUIContent("Float Direction", "What direction the float effect goes."));
				EditorGUILayout.PropertyField(floatLoop, new GUIContent("Float Loop", "If the floating effect loops back to its original after."));
				if (_targetInstance.FloatLoop)
				{
					EditorGUILayout.PropertyField(floatLoopMaxTime, new GUIContent("Float Loop Max Time", "The maximum time before looping the effect."));
					EditorGUILayout.PropertyField(floatLoopMessages, new GUIContent("Float Loop Messages", "If the float effect loops through different messages."));
				}
				EditorGUILayout.PropertyField(floatFadeTextOverTime, new GUIContent("Float Fade Text", "If the floating effect fades the text over time."));
				if (_targetInstance.FloatFadeTextOverTime)
				{
					EditorGUILayout.PropertyField(floatFadeFromTransparent, new GUIContent("Float Fade From Transparent", "If the floating fade text starts transparent."));
					EditorGUILayout.PropertyField(floatFadeSpeed, new GUIContent("Float Fade Speed", "How fast the floating text fades in our out."));
				}
			}

			GUILayout.Space(10f);

			GUILayout.Label("[Events]", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hasHoverEvents, new GUIContent("Enter Events Enabled", "If this UIElement should call external events on hovering."));
			if (_targetInstance.HasHoverEvents)
			{
				EditorGUILayout.PropertyField(hoverEvents, new GUIContent("Hover Events", "The events to invoke when hovering."));
			}

			GUILayout.Space(10f);

			EditorGUILayout.PropertyField(hasHoverExitEvents, new GUIContent("Exit Events Enabled", "If this UIElement should call external events on exit hovering."));
			if (_targetInstance.HasHoverExitEvents)
			{
				EditorGUILayout.PropertyField(hoverExitEvents, new GUIContent("Hover Exit Events", "The events to invoke when exit hovering."));
			}

			serializedObject.ApplyModifiedProperties();

			base.OnInspectorGUI();
		}
	}
}
