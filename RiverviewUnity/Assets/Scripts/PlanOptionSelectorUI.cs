using UnityEngine;
using System.Collections.Generic;
using NotABear;

public class PlanOptionSelectorUI : MonoBehaviour
{
	[SerializeField]
	private PlanUI planUI;

	[SerializeField]
	private Transform optionsContainer;

	[SerializeField]
	private PlanOptionUI optionUIPrefab;

	//private PlanOptionsLoadout loadout;
	private List<GameObject> optionUIs;

	public void Initialise(PlanOptionsLoadout loadout)
	{
		//this.loadout = loadout;

		Clear();
		planUI.Clear();

		for (int i = 0; i < loadout.planOptions.Count; ++i)
		{
			PlanOptionUI newUI = Object.Instantiate(optionUIPrefab, optionsContainer);
			newUI.Initialise(loadout.planOptions[i]);
			newUI.Selected += SelectOption;
		}
	}

	public void Clear()
	{
		for (int i = 0; i < optionUIs.Count; ++i)
		{
			Object.Destroy(optionUIs[i]);
		}
		optionUIs.Clear();
	}

	public void AddDeselectedOption(PlanOptionUI option)
	{
			option.Selected += SelectOption;
			option.transform.SetParent(optionsContainer);
	}

	public void SelectOption(PlanOptionUI option)
	{
		optionUIs.Remove(option.gameObject);
		option.Selected -= SelectOption;
		planUI.AddSelectedOption(option);
	}
}
