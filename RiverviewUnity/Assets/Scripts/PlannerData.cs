using UnityEngine;

namespace NotABear
{

	[System.Serializable]
	public abstract class DataItem : IDataItem
	{
		public string name { get { return m_name; } set { m_name = value; } }
		[SerializeField]
		private string m_name;
	}

	public interface IDataItem
	{
		string name { get; set; }
	}

	[CreateAssetMenu(fileName="PlannerData.asset", menuName="Riverview/Planner Data")]
	public class PlannerData : ScriptableObject
	{
		public PlannerItemData[] items;

		public CharacterStat[] characterStats;
		public SubjectData[] subjects;
		public PlanActivityData[] planActivities;
	}

	[System.Serializable]
	public class PlannerItemData : DataItem
	{
		public int timeUnits;
		[EnumFlag]
		public SlotType validSlots;
		public SubjectData subject;
		public PlanActivityData activity;
	}

}
