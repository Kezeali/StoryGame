 using UnityEngine;
 using UnityEditor;

 [CustomPropertyDrawer(typeof(SceneField))]
 public class SceneFieldPropertyDrawer : PropertyDrawer 
 {
	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
	{
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty sceneAsset = _property.FindPropertyRelative("sceneAsset");
		SerializedProperty scenePath = _property.FindPropertyRelative("scenePath");
		_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		if (sceneAsset != null)
		{
			sceneAsset.objectReferenceValue = EditorGUI.ObjectField(_position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false); 
			if (sceneAsset.objectReferenceValue != null)
			{
				scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset.objectReferenceValue);
			}
		}
		EditorGUI.EndProperty();
	}
 }
