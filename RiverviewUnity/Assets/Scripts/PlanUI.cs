using UnityEngine;
using NotABear;
using System.Collections.Generic;

public class PlanUI : MonoBehaviour
{
	[SerializeField]
	private Transform[] optionsContainers;

	public PlanOptionSelectorUI planOptionSelectorUI;
	private List<GameObject> optionUIs;

	public void Clear()
	{
		for (int i = 0; i < optionUIs.Count; ++i)
		{
			Object.Destroy(optionUIs[i]);
		}
		optionUIs.Clear();
	}

	public void AddSelectedOption(PlanOptionUI option)
	{
		// Look for an empty slot (in a dumb way)
		Transform optionsContainer = null;
		for (int i = 0; i < optionsContainers.Length; ++i)
		{
			if (optionsContainers[i].childCount == 0)
			{
				optionsContainer = optionsContainers[i];
				break;
			}
		}
		if (optionsContainer != null)
		{
			option.Selected += SelectOption;
			option.transform.SetParent(optionsContainer);
		}
	}

	public void SelectOption(PlanOptionUI option)
	{
		optionUIs.Remove(option.gameObject);
		option.Selected -= SelectOption;
		planOptionSelectorUI.AddDeselectedOption(option);
	}
}
