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
	private DataItemSource dataItemSource;

	public void Awake()
	{
		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>();
		System.Array.Sort(uiSections, PlanSectionUI.Compare);
	}

	public void Initialise(Plan loadedPlan, DataItemSource dataItemSource)
	{
		this.dataItemSource = dataItemSource;

		this.plan = new Plan();
		this.plan.name = planName;

		int sectionsCount = this.uiSections.Length;

		this.plan.sections = new PlanSection[sectionsCount];
		for (int newSectionIndex = 0; newSectionIndex < sectionsCount; ++newSectionIndex)
		{
			var planSection = new PlanSection();
			this.plan.sections[newSectionIndex] = planSection;

			PlanSectionUI uiSection = uiSections[newSectionIndex];
			//planSection.name = uiSection.name;

			int slotsCount = uiSection.slots.Length;

			planSection.slots = new PlanSlot[slotsCount];
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];

				var newSlot = new PlanSlot();
				newSlot.unitIndex = slotUI.SlotUnitIndex();
				newSlot.slotType = slotUI.slotType;
				planSection.slots[slotIndex] = newSlot;
			}

			// Upgrade the data
			if (loadedPlan != null && loadedPlan.sections.Length > newSectionIndex)
			{
				PlanSection loadedSection = loadedPlan.sections[newSectionIndex];

				SlotType fillingSlotType = SlotType.None;
				int fillingSlotIndex = 0;

				for (int loadedSlotIndex = 0; loadedSlotIndex < loadedSection.slots.Length; ++loadedSlotIndex)
				{
					PlanSlot loadedSlot = loadedSection.slots[loadedSlotIndex];

					if (fillingSlotType == SlotType.None)
					{
						fillingSlotType = uiSection.slots[loadedSlotIndex].slotType;
					}
					planSection.slots[fillingSlotIndex] = loadedSlot;
				}
			}
		}

		Save();
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

	public void Save()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		Serialiser.Serialise(sb, this.plan, this.dataItemSource);

		System.IO.File.WriteAllText("save.txt", sb.ToString());
	}
}

}
