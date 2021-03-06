using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

	// Asset type which just lists planner & calendar related assets. See also DataIndex
	[CreateAssetMenu(fileName="PlannerDataIndex.asset", menuName="Cloverview/Planner Data Index")]
	public class PlannerDataIndex : ScriptableObject
	{
		[Reorderable]
		public PlannerItemData[] items;

		// Ordered list of calendars. These could be yearly or term-ly calendars, or whatever else is useful in terms of making re-usable sequences of planning periods / plan schemas
		[Reorderable]
		public CalendarDefinition[] calendars;
		
		public SubjectData[] subjects;
		public PlanActivityData[] planActivities;
		public EventData[] events;
		public StageMarkData[] stageMarks;
	}

	[System.Serializable]
	public class PlannerItemData : DataItem
	{
		public int timeUnits;
		[EnumFlag]
		public SlotType validSlots;
		public QualityData[] requiredQualityTags;
		public SubjectData subject;
		public PlanActivityData activity;
		public StatBonusData[] statBonuses;
		public StatAffectorData[] statAffectors;

		public override string ToString()
		{
			return this.name + ": subject=" + subject.name + ", activity=" + activity.name;
		}
	}

}
