using UnityEngine;

namespace NotABear
{

public class SpecifiedDurationPlanSlotUI : PlanSlotUI
{
	[System.NonSerialized]
	public int unitIndex;

	public int durationInUnits;

	public override int SlotUnitIndex() { return unitIndex; }
}

}
