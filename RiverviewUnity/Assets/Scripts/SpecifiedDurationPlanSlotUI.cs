using UnityEngine;

namespace Cloverview
{

public class SpecifiedDurationPlanSlotUI : PlanSlotUI
{
	public int firstUnitOffset;
	public int durationInUnits;

	[System.NonSerialized]
	public int unitIndex;

	public override int SlotUnitIndex() { return unitIndex; }
}

}
