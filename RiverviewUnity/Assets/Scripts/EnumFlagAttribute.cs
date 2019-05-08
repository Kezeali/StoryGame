using UnityEngine;

// Attribute which can be added to fields in MonoBehaviours, so the custom property drawer (EnumFlagDrawer) will render it in inspectors. 
public class EnumFlagAttribute : PropertyAttribute
{
	public string enumName;

	public EnumFlagAttribute() {}

	public EnumFlagAttribute(string name)
	{
		enumName = name;
	}
}