using UnityEngine;

namespace NotABear
{

public class SequentialPlanSlotUIGroup : MonoBehaviour
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
}

}
