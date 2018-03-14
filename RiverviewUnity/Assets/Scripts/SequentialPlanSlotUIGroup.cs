using UnityEngine;

namespace Cloverview
{

public class SequentialPlanSlotUIGroup : MonoBehaviour, IPlanSlotUIGroup
{
	[SerializeField]
	private int totalUnits;

	[System.NonSerialized]
	public SequentialPlanSlotUI[] slots;

	public void Awake()
	{
		this.slots = this.GetComponentsInChildren<SequentialPlanSlotUI>();

		int slotDurationUnits = totalUnits / slots.Length;
		for (int i = 0; i < slots.Length; ++i)
		{
			slots[i].unitIndex = slotDurationUnits * i;
		}
	}

	public int TotalTimeUnits()
	{
		return this.totalUnits;
	}
}

}
