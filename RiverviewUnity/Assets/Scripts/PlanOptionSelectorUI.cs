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

	private PlannerData plannerData;
	private List<PlanOptionUI> optionUIs = new List<PlanOptionUI>();

	private PlanOptionsLoadout currentLoadout;
	private PlanOptionsLoadout rootLoadout;

	public void Initialise(PlannerData plannerData)
	{
		this.plannerData = plannerData;

		Clear();
		planUI.Clear();

		this.rootLoadout = MakeRootLoadout();
	}

	public void Populate(PlanSlotUI slot)
	{
		SlotType type = slot.slotType;

		Clear();

		this.currentLoadout = MakeDerrivedLoadout(this.rootLoadout, type);

		for (int i = 0; i < currentLoadout.planOptions.Count; ++i)
		{
			PlanOptionUI newUI = Object.Instantiate(optionUIPrefab, optionsContainer);
			newUI.Initialise(currentLoadout.planOptions[i]);
			newUI.Selected += SelectOption;
			optionUIs.Add(newUI);
		}
	}

	public PlanOptionsLoadout MakeDerrivedLoadout(PlanOptionsLoadout rootLoadout, SlotType type)
	{
		var newLoadout = new PlanOptionsLoadout();
		newLoadout.name = type.ToString();

		newLoadout.planOptions = new List<PlanOption>(rootLoadout.planOptions.Count);

		for (int i = 0; i < rootLoadout.planOptions.Count; ++i)
		{
			PlanOption option = rootLoadout.planOptions[i];
			if ((option.plannerItem.validSlots & type) != 0)
			{
				newLoadout.planOptions.Add(option);
			}
		}
		return newLoadout;
	}

	public PlanOptionsLoadout MakeRootLoadout()
	{
		var newLoadout = new PlanOptionsLoadout();
		newLoadout.name = "root";

		newLoadout.planOptions = new List<PlanOption>(this.plannerData.items.Length);

		for (int i = 0; i < this.plannerData.items.Length; ++i)
		{
			PlannerItemData item = this.plannerData.items[i];
			{
				PlanOption option = new PlanOption();
				option.plannerItem = item;
				newLoadout.planOptions.Add(option);
			}
		}
		return newLoadout;
	}

	public void Clear()
	{
		for (int i = 0; i < optionUIs.Count; ++i)
		{
			Object.Destroy(optionUIs[i].gameObject);
		}
		optionUIs.Clear();
	}

	public void DeselectOption(PlanOption option)
	{
		for (int i = 0; i < optionUIs.Count; ++i)
		{
			PlanOptionUI optionUI = optionUIs[i];
			if (optionUI.planOption == option)
			{
				optionUI.EnableSelection();
			}
		}
	}

	public void SelectOption(PlanOptionUI option)
	{
		planUI.SelectOption(option);
	}
}
