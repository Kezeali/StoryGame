using UnityEngine;

namespace Cloverview
{

public class PlanSectionUI : MonoBehaviour
{
	[ReadOnly]
	public PlanSlotUI[] slots;

	public void OnEnable()
	{
		if (this.slots == null)
		{
			this.GatherSlots();
		}
	}

	public void GatherSlots()
	{
		this.slots = this.GetComponentsInChildren<PlanSlotUI>(true);
		var planSlotUIGroup = this.GetComponent<IPlanSlotUIGroup>();
		if (planSlotUIGroup != null)
		{
			planSlotUIGroup.Initialise();
		}
	}

	public virtual int SectionUnitIndex()
	{
		return this.transform.GetSiblingIndex();
	}

	public virtual int TotalTimeUnits()
	{
		var planSlotUIGroup = this.GetComponent<IPlanSlotUIGroup>();
		if (planSlotUIGroup != null)
		{
			return planSlotUIGroup.TotalTimeUnits();
		}
		else
		{
			return slots.Length;
		}
	}

	public static int Compare(PlanSectionUI a, PlanSectionUI b)
	{
		if (a.SectionUnitIndex() < b.SectionUnitIndex())
		{
			return -1;
		}
		else if (a.SectionUnitIndex() > b.SectionUnitIndex())
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}
}

public interface IPlanSlotUIGroup
{
	void Initialise();
	int TotalTimeUnits();
}

}
