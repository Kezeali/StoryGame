using UnityEngine;

namespace NotABear
{

public class SequentialPlanSlotUI : PlanSlotUI
{
	[System.NonSerialized]
	public int unitIndex;

	public override int SlotUnitIndex() { return unitIndex; }
}

}