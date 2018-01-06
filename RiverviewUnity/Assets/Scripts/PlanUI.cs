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
	}

	public void Start()
	{
		plan = new Plan();
		plan.name = planName;
		plan.sections = new PlanSection[this.uiSections.Length];
		for (int i = 0; i < plan.sections.Length; ++i)
		{
			var planSection = new PlanSection();
			plan.sections[i] = planSection;
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
