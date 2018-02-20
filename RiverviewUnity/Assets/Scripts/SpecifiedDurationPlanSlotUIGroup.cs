using UnityEngine;

namespace NotABear
{

public class SpecifiedDurationPlanSlotUIGroup : MonoBehaviour
{
	[System.NonSerialized]
	public SpecifiedDurationPlanSlotUI[] slots;

	public void Awake()
	{
		this.slots = this.GetComponentsInChildren<SpecifiedDurationPlanSlotUI>();

		int unitIndex = 0;
		for (int i = 0; i < slots.Length; ++i)
		{
			SpecifiedDurationPlanSlotUI slot = slots[i];
			unitIndex += slot.firstUnitOffset;
			slot.unitIndex = unitIndex;
			unitIndex += slot.durationInUnits;
		}
	}
}

}
