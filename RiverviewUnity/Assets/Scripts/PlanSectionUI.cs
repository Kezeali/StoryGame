using UnityEngine;

namespace NotABear
{

public class PlanSectionUI : MonoBehaviour
{
	[System.NonSerialized]
	public PlanSlotUI[] slots;

	public void Awake()
	{
		this.slots = this.GetComponentsInChildren<PlanSlotUI>();
	}

	public virtual int PlanSectionUnitIndex()
	{
		// TODO(elliot): however you determine the child index of a transform:
		return 0;//this.transform.childIndex;
	}

	public static int Compare(PlanSectionUI a, PlanSectionUI b)
	{
		if (a.PlanSectionUnitIndex() < b.PlanSectionUnitIndex())
		{
			return -1;
		}
		else if (a.PlanSectionUnitIndex() > b.PlanSectionUnitIndex())
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
