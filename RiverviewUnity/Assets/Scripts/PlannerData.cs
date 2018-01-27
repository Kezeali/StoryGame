using UnityEngine;

namespace NotABear
{

	[System.Serializable]
	public abstract class DataItem
	{
		public string name;
	}

	[CreateAssetMenu(fileName="PlannerData.asset", menuName="Riverview/Planner Data")]
	public class PlannerData : ScriptableObject
	{
		public PlannerItemData[] items;
	}

	[System.Serializable]
	public class PlannerItemData : DataItem
	{
		public int timeUnits;
		public SlotType validSlots;
		public PlanActivityData activity;
	}

}
