using UnityEngine;

namespace NotABear
{

[System.Flags]
public enum SlotType
{
	None = 0,
	School = 1 << 0,
	FreeTime = 1 << 1,
}

public class PlanSlotUI : MonoBehaviour
{
	public SlotType slotType;

	public virtual int SlotUnitIndex()
	{
		return this.transform.GetSiblingIndex();
	}

	public static int Compare(PlanSlotUI a, PlanSlotUI b)
	{
		if (a.SlotUnitIndex() < b.SlotUnitIndex())
		{
			return -1;
		}
		else if (a.SlotUnitIndex() > b.SlotUnitIndex())
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}
}

}
