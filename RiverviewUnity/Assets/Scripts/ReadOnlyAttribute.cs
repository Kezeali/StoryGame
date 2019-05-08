using UnityEngine;

// Attribute which marks a Monobehaviour field to be drawn with ReadOnlyAttributeDrawer in the inspector.
[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class ReadOnlyAttribute : PropertyAttribute
{
}
