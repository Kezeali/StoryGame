using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

[CreateAssetMenu(fileName="CalendarDefinition.asset", menuName="Cloverview/Calendar Definition")]
public class CalendarDefinition : SerializedObject, IDataItem 
{
	public PlanningStream[] streams;

	int totalTimeUnits = -1;
	public int GetTotalTimeUnits()
	{
		if (this.totalTimeUnits == -1) {
			for (int streamIndex = 0; streamIndex < this.streams.Length; ++streamIndex) {
				PlanningStream stream = this.streams[i];
				for (int periodIndex = 0; periodIndex < stream.planningPeriods.Length; ++periodIndex) {
					CalendarPeriod period = stream.planningPeriods[periodIndex];
					this.totalTimeUnits += period.schema.totalTimeUnits;
				}
			}
		}
		return this.totalTimeUnits;
	}

	public static Calendar FindCalendar(Calendar[] calendars, int now)
	{
		Calendar result = null;
		for (int calendarIndex = 0; calendarIndex < calendars.Length; ++calendarIndex) {
			Calendar calendar = calendars[calendarIndex];
			for (int streamIndex = 0; streamIndex < calendar.streams.Length; ++streamIndex) {
				PlanningStream stream = calendar.streams[i];
				for (int periodIndex = 0; periodIndex < stream.planningPeriods.Length; ++periodIndex) {
					CalendarPeriod period = stream.planningPeriods[periodIndex];
					
				}
			}
		}
	}
}

public struct PlanningStream
{
	public string name;
	[Reorderable]
	public CalendarPeriod[] planningPeriods;
}

public struct CalendarPeriod
{
	public string title;
	public string schemaName;
}

}
