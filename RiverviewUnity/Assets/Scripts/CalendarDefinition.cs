using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

[CreateAssetMenu(fileName="CalendarDefinition.asset", menuName="Cloverview/Calendar Definition")]
public class CalendarDefinition : ScriptableObject, IDataItem 
{
	[Reorderable]
	public PlanningPeriod[] planningPeriods;

	int totalTimeUnits = -1;
	// Calculates the total time value or returns the cached value if it's available
	public int GetTotalTimeUnits()
	{
		if (this.totalTimeUnits == -1) {
			for (int periodIndex = 0; periodIndex < this.planningPeriods.Length; ++periodIndex) {
				PlanningPeriod period = this.planningPeriods[periodIndex];
				Debug.Assert(period.schema != null);
				if (period.schema != null) {
					this.totalTimeUnits += period.schema.GetTotalTimeUnits();
				}
			}
		}
		return this.totalTimeUnits;
	}
}

[System.Serializable]
public struct PlanningPeriod
{
	public string title;
	public PlanSchema schema;
}

}
