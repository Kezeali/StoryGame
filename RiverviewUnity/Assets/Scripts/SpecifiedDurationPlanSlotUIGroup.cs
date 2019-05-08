using UnityEngine;

namespace Cloverview
{

// Defines a group of plan slots where each individual slot has a specified duration. See also SpecifiedDurationPlanSlotUI.
public class SpecifiedDurationPlanSlotUIGroup : MonoBehaviour, IPlanSlotUIGroup
{
	[System.NonSerialized]
	public SpecifiedDurationPlanSlotUI[] slots;

	int totalTimeUnits;

	public void Awake()
	{
		if (this.slots == null)
		{
			this.Initialise();
		}
	}

	public void Initialise()
	{
		this.slots = this.GetComponentsInChildren<SpecifiedDurationPlanSlotUI>();

		int unitIndex = 0;
		for (int i = 0; i < slots.Length; ++i)
		{
			SpecifiedDurationPlanSlotUI slot = slots[i];
			unitIndex += slot.firstUnitOffset;
			slot.start = unitIndex;
			unitIndex += slot.durationInUnits;
		}

		this.totalTimeUnits = unitIndex;
	}

	public int TotalTimeUnits()
	{
		if (this.totalTimeUnits == 0)
		{
			this.Awake();
		}
		return this.totalTimeUnits;
	}
}

}
