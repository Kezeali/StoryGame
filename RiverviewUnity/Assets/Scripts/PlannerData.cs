using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

	[CreateAssetMenu(fileName="PlannerData.asset", menuName="Cloverview/Planner Data")]
	public class PlannerData : ScriptableObject
	{
		[Reorderable]
		public PlannerItemData[] items;
		// Ordered list of calendars for each in-game year
		[Reorderable]
		public Calendar[] calendars;

		public CharacterStatDefinition[] characterStats;
		public SubjectData[] subjects;
		public PlanActivityData[] planActivities;
		public EventData[] events;
	}

	[System.Serializable]
	public class PlannerItemData : DataItem
	{
		public int timeUnits;
		[EnumFlag]
		public SlotType validSlots;
		public SubjectData subject;
		public PlanActivityData activity;

		public override string ToString()
		{
			return this.name + ": subject=" + subject.name + ", activity=" + activity.name;
		}
	}

}
