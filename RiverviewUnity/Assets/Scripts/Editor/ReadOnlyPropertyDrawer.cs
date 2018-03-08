using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
	{
		bool wasEnabled = GUI.enabled;
		GUI.enabled = false;
		EditorGUI.PropertyField(rect, prop, label);
		GUI.enabled = wasEnabled;
	}
}