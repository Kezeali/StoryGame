using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnumFlagAttribute))]
public class EnumFlagDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EnumFlagAttribute flagSettings = (EnumFlagAttribute)attribute;
		Enum targetEnum = (Enum)GetTargetObjectOfProperty(property);

		string propName = flagSettings.enumName;
		if (string.IsNullOrEmpty(propName))
			propName = property.name;

		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.BeginChangeCheck();
		Enum enumNew = EditorGUI.EnumFlagsField(position, propName, targetEnum);
		if (EditorGUI.EndChangeCheck() || !property.hasMultipleDifferentValues)
		{
			property.intValue = (int)Convert.ChangeType(enumNew, targetEnum.GetType());
		}
		EditorGUI.EndProperty();
	}

	public static object GetTargetObjectOfProperty(SerializedProperty prop)
	{
		var path = prop.propertyPath.Replace(".Array.data[", "[");
		object obj = prop.serializedObject.targetObject;
		var elements = path.Split('.');
		foreach (var element in elements)
		{
			if (element.Contains("["))
			{
				var elementName = element.Substring(0, element.IndexOf("["));
				var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
				obj = GetValue_Imp(obj, elementName, index);
			}
			else
			{
				obj = GetValue_Imp(obj, element);
			}
		}
		return obj;
	}

	private static object GetValue_Imp(object source, string name)
	{
		if (source == null)
			return null;
		var type = source.GetType();

		while (type != null)
		{
			var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (f != null)
				return f.GetValue(source);

			var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (p != null)
				return p.GetValue(source, null);

			type = type.BaseType;
		}
		return null;
	}

	private static object GetValue_Imp(object source, string name, int index)
	{
		var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
		if (enumerable == null) return null;
		var enm = enumerable.GetEnumerator();
		for (int i = 0; i <= index; i++)
		{
			if (!enm.MoveNext()) return null;
		}
		return enm.Current;
	}
}
