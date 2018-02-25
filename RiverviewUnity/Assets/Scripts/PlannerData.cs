using UnityEngine;
using SubjectNerd.Utilities;

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

	[CreateAssetMenu(fileName="PlannerData.asset", menuName="Cloverview/Planner Data")]
	public class PlannerData : ScriptableObject
	{
		[Reorderable]
		public PlannerItemData[] items;

		public CharacterStatDefinition[] characterStats;
		public SubjectData[] subjects;
		public PlanActivityData[] planActivities;
		public QualityData[] qualities;
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
