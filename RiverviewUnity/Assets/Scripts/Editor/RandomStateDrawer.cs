using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Random.State))]
public class RandomStateDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.PropertyField(position, property, label);
		// TODO: seed editor
		EditorGUI.EndProperty();
	}
}
