using UnityEngine;
using NotABear;
using System.Collections.Generic;

namespace NotABear
{

public class PlanUI : MonoBehaviour
{
	[SerializeField]
	private string planName;

	[SerializeField]
	private PlanOptionSelectorUI planOptionSelectorUI;

	private Plan plan;
	private PlanSectionUI[] uiSections;
	private List<GameObject> optionUIs = new List<GameObject>();

	public void Awake()
	{
		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>();
		System.Array.Sort(uiSections, PlanSectionUI.Compare);
	}

	public void Initialise(Plan loadedPlan)
	{
		this.plan = new Plan();
		this.plan.name = planName;
		this.plan.sections = new PlanSection[this.uiSections.Length];
		for (int newSectionIndex = 0; newSectionIndex < this.plan.sections.Length; ++newSectionIndex)
		{
			var planSection = new PlanSection();
			this.plan.sections[newSectionIndex] = planSection;
			if (loadedPlan != null && loadedPlan.sections.Length > newSectionIndex)
			{
				PlanSection loadedSection = loadedPlan.sections[newSectionIndex];
				for (int loadedSlotIndex = 0; loadedSlotIndex < loadedSection.filledSlots.Count; ++loadedSlotIndex)
				{
					planSection.filledSlots.Add(loadedSection.filledSlots[loadedSlotIndex]);
				}
			}
		}
	}

	public void Clear()
	{
		for (int i = 0; i < this.optionUIs.Count; ++i)
		{
			Object.Destroy(this.optionUIs[i]);
		}
		this.optionUIs.Clear();
	}

	public void SelectOption(PlanOptionUI option)
	{
		optionUIs.Remove(option.gameObject);
		option.Selected -= SelectOption;
		planOptionSelectorUI.AddDeselectedOption(option);
	}
}

}
