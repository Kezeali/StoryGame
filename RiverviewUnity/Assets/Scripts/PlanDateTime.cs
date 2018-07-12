using UnityEngine;

namespace Cloverview
{

[System.Serializable]
public struct PlanDateTime
{
	public int timeUnits;

	// For weekly plans, this is the hour of the week
	public int timeUnitsSinceCurrentPlanningPeriodBegan;
	// For plans where the sections are days, this is the hour of the day
	public int timeUnitsSinceCurrentSectionBegan;

	// If timeUnits value is greater the total time units defined in all the calendar definitions in the given planning stream, this number will be increased
	public int calendarSequenceLoops;

	public int calendarIndex;
	public int planningPeriodIndex;
	public int sectionIndex;
	public int slotIndex;
	public PlannerDataIndex plannerData;

	public CalendarDefinition GetCalendar()
	{
		if (this.plannerData != null) {
			return this.plannerData.calendars[this.calendarIndex];
		} else {
			return null;
		}
	}
	
	public PlanningPeriod GetPlanningPeriod()
	{
		if (this.plannerData != null) {
			return this.plannerData.calendars[this.calendarIndex].planningPeriods[this.planningPeriodIndex];
		} else {
			return default(PlanningPeriod);
		}
	}
	
	public PlanSchema GetSchema()
	{
		if (this.plannerData != null) {
			return this.plannerData.calendars[this.calendarIndex].planningPeriods[this.planningPeriodIndex].schema;
		} else {
			return null;
		}
	}
	
	public PlanSchemaSection GetSchemaSection()
	{
		if (this.plannerData != null) {
			return this.plannerData.calendars[this.calendarIndex].planningPeriods[this.planningPeriodIndex].schema.sections[this.sectionIndex];
		} else {
			return null;
		}
	}
	
	public PlanSchemaSlot GetSchemaSlot()
	{
		if (this.plannerData != null) {
			if (this.slotIndex >= 0) {
				PlanSchemaSection section = this.plannerData.calendars[this.calendarIndex].planningPeriods[this.planningPeriodIndex].schema.sections[this.sectionIndex];
				if (this.slotIndex < section.slots.Length) {
					return section.slots[this.slotIndex];
				}
			}
		}
		return null;
	}
	
	public bool HasPlanningSlot()
	{
		return this.GetSchemaSlot() != null;
	}

	public PlanDateTime GetSubsequentPlanningPeriod(int periods)
	{
		PlanDateTime result = this;
		for (int i = 0; i < periods; ++i) {
			PlanSchema currentSchema = result.GetSchema();
			int remainingTime = currentSchema.GetTotalTimeUnits() - result.timeUnitsSinceCurrentPlanningPeriodBegan;
			result = PlanDateTime.FromTimeUnitsOffset(this, remainingTime);
		}
		return result;
	}

	public static PlanDateTime FromTimeUnits(PlannerDataIndex plannerData, int now)
	{
		PlanDateTime basis = new PlanDateTime() { plannerData = plannerData };
		return PlanDateTime.FromTimeUnitsOffset(basis, now);
	}

	public static PlanDateTime FromTimeUnitsOffset(PlanDateTime basis, int offset)
	{
		Debug.Assert(basis.plannerData != null);
		UnityEngine.Profiling.Profiler.BeginSample("PlanDateTime.FromTimeUnitsOffset");
		PlanDateTime result = basis;
		result.timeUnits = basis.timeUnits + offset;
		// Repeatedly iterate through calendar definitions until the time is used up
		int calendarSequenceLoops = result.calendarSequenceLoops;
		while (offset >= 0) {
			for (int calendarIndex = basis.calendarIndex; calendarIndex < basis.plannerData.calendars.Length; ++calendarIndex) {
				CalendarDefinition calendar = basis.plannerData.calendars[calendarIndex];

				// Get the cached total time value and avoid searching calendars that the time can't fall within
				int totalTimeUnitsInCalendar = calendar.GetTotalTimeUnits();
				if (offset - totalTimeUnitsInCalendar < 0) {
					
					result.calendarIndex = calendarIndex;

					for (int periodIndex = basis.planningPeriodIndex; periodIndex < calendar.planningPeriods.Length; ++periodIndex) {
						PlanningPeriod period = calendar.planningPeriods[periodIndex];
						PlanSchema schema = period.schema;

						int totalTimeUnitsInSchema = schema.GetTotalTimeUnits();
						if (offset - totalTimeUnitsInSchema < 0) {
							int timeUnitsSinceCurrentPlanningPeriodBegan = offset;

							for (int sectionIndex = basis.sectionIndex; sectionIndex < schema.sections.Length; ++sectionIndex) {
								PlanSchemaSection schemaSection = schema.sections[sectionIndex];
								if (offset - schemaSection.totalTimeUnits < 0) {
									
									result.timeUnitsSinceCurrentPlanningPeriodBegan = timeUnitsSinceCurrentPlanningPeriodBegan;
									result.planningPeriodIndex = periodIndex;

									result.timeUnitsSinceCurrentSectionBegan = offset;
									result.sectionIndex = sectionIndex;
									
									for (int slotIndex = basis.slotIndex; slotIndex < schemaSection.slots.Length; ++slotIndex) {
										PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];
										if (offset - schemaSlot.unitLength < 0) {
											result.slotIndex = slotIndex;
											break;
										}
										offset -= schemaSlot.unitLength;
									}
									// There may be no slot at this time, but the search is done regardless
									offset = -1;
									break;
								}
								offset -= schemaSection.totalTimeUnits;
							}
						}
						if (offset < 0) {
							// Search is done
							break;
						}
						offset -= totalTimeUnitsInSchema;
					}
				}
				if (offset < 0) {
					// Search is done
					break;
				}
				offset -= totalTimeUnitsInCalendar;
			}
			if (offset < 0) {
				// Search is done
				break;
			}
			++calendarSequenceLoops;
		}
		result.calendarSequenceLoops = calendarSequenceLoops;
		UnityEngine.Profiling.Profiler.EndSample();
		return result;
	}
}

}
