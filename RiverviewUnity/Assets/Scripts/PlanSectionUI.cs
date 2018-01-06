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
}

}