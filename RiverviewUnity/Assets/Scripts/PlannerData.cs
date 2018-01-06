using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="PlannerData.asset", menuName="Riverview/Planner Data")]
	public class PlannerData : ScriptableObject
	{
		public PlannerItemData[] items;
	}

	[System.Serializable]
	public class PlannerItemData
	{
		public string name;
		public int timeUnits;
	}

}