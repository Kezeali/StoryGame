 using UnityEngine;
 using UnityEditor;

 [CustomPropertyDrawer(typeof(SceneField))]
 public class SceneFieldPropertyDrawer : PropertyDrawer 
 {
	public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
	{
		EditorGUI.BeginProperty(_position, GUIContent.none, _property);
		SerializedProperty scenePath = _property.FindPropertyRelative("scenePath");

    var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath.stringValue);

		_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
		SceneAsset sceneAsset = EditorGUI.ObjectField(_position, oldScene, typeof(SceneAsset), false) as SceneAsset;
		if (sceneAsset != null)
		{
			scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
		}
		else
		{
			scenePath.stringValue = "";
		}
		EditorGUI.EndProperty();
	}
 }
