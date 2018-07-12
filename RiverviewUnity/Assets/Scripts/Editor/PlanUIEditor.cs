using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cloverview
{
[CustomEditor(typeof(PlanUI))]
[CanEditMultipleObjects]
public sealed class PlanUIEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (GUILayout.Button("Generate Schema")) {
			foreach (PlanUI planUi in targets) {
				PlanUI.GenerateOrRefreshSchema(planUi);
			}
		}
	}
}

}
