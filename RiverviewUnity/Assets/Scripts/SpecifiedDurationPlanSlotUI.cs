using UnityEngine;

namespace Cloverview
{

// Defines a plan slot UI which has a specified duration.
public class SpecifiedDurationPlanSlotUI : PlanSlotUI
{
	public int firstUnitOffset;
	public int durationInUnits;

	// TODO(elliot): make this stuff editor-only, since it is now saved in the schema
	[System.NonSerialized]
	public int start;
	// TODO(elliot): make this stuff editor-only, since it is now saved in the schema
	public override int GetStartTime() { return this.start; }

	public override int GetDuration() { return this.durationInUnits; }
}

}
