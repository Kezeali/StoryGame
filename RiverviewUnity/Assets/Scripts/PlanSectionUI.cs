using UnityEngine;

namespace Cloverview
{

public class PlanSectionUI : MonoBehaviour
{
	[System.NonSerialized]
	public PlanSlotUI[] slots;

	public void OnEnable()
	{
		this.slots = this.GetComponentsInChildren<PlanSlotUI>();
	}

	public virtual int SectionUnitIndex()
	{
		return this.transform.GetSiblingIndex();
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

}
