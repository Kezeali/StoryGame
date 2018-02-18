using UnityEngine;
using NotABear;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NotABear
{

public class PlanUI : MonoBehaviour
{
	[SerializeField]
	private string planName;

	[SerializeField]
	private PlanOptionSelectorUI planOptionSelectorUI;

	[SerializeField]
	private PlanOptionUI defaultFilledSlotPrefab;

	private Plan plan;
	private PlanSectionUI[] uiSections;
	private List<GameObject> optionUIs = new List<GameObject>();

	private PlanSlotUI selectedSlot;

	public void Awake()
	{
		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>();
		System.Array.Sort(uiSections, PlanSectionUI.Compare);
	}

	public void Initialise(SaveData loadedData)
	{
		Plan loadedPlan = loadedData.weeklyPlan;

		this.selectedSlot = null;

		// NOTE(ehayward): it is important the the plan is a new or cleared plan at this point
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

				slotUI.Initialise(newSlot);
				slotUI.clicked += this.OnSlotClicked;
			}

			// Upgrade the data
			if (loadedPlan != null && loadedPlan.sections.Length > newSectionIndex)
			{
				PlanSection loadedSection = loadedPlan.sections[newSectionIndex];

				// Try to transfer the content of each slot
				for (int loadedSlotIndex = 0; loadedSlotIndex < loadedSection.slots.Length; ++loadedSlotIndex)
				{
					PlanSlot loadedSlot = loadedSection.slots[loadedSlotIndex];

					int unitBegin = loadedSlot.unitIndex;
					int unitEnd = unitBegin;
					if (loadedSlot.selectedOption != null && loadedSlot.selectedOption.plannerItem != null)
					{
						unitEnd = unitBegin + loadedSlot.selectedOption.plannerItem.timeUnits;
					}

					SlotType requiredType = loadedSlot.slotType;

					// Look for the first valid slot that overlaps the given entry
					for (int actualSlotIndex = 0; actualSlotIndex < planSection.slots.Length; ++ actualSlotIndex)
					{
						PlanSlot actualSlot = planSection.slots[actualSlotIndex];
						if (actualSlot.slotType == requiredType && actualSlot.selectedOption == null)
						{
							int actualSlotUnitEnd = unitEnd; // Default to allowing anything in the last slot
							if (actualSlotIndex < planSection.slots.Length-1)
							{
								PlanSlot nextActualSlot = null;
								nextActualSlot = planSection.slots[actualSlotIndex+1];
								actualSlotUnitEnd = nextActualSlot.unitIndex - 1;
							}
							if (
								(actualSlot.unitIndex <= unitBegin && actualSlotUnitEnd > unitBegin) ||
								(actualSlot.unitIndex < unitEnd && actualSlotUnitEnd >= unitEnd))
							{
								actualSlot.selectedOption = loadedSlot.selectedOption;
							}
						}
					}
				}
			}

			// Populate the slot UI elements
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];
				slotUI.Populate(this.defaultFilledSlotPrefab);
			}
		}

		loadedData.weeklyPlan = this.plan;
	}

	public void Clear()
	{
		for (int i = 0; i < this.optionUIs.Count; ++i)
		{
			Object.Destroy(this.optionUIs[i]);
		}
		this.optionUIs.Clear();

		this.selectedSlot = null;
	}

	public void SelectOption(PlanOptionUI optionUi)
	{
		if (this.selectedSlot != null)
		{
			optionUi.DisableSelection();
			this.selectedSlot.Fill(optionUi.planOption, this.defaultFilledSlotPrefab);
		}
	}

	public void OnSlotClicked(PlanSlotUI slot)
	{
		if (this.selectedSlot != slot)
		{
			this.selectedSlot = slot;
			this.planOptionSelectorUI.Populate(slot);
		}
		else
		{
			this.planOptionSelectorUI.DeselectOption(slot.dataSlot.selectedOption);
			this.selectedSlot.Clear();
			this.planOptionSelectorUI.Populate(slot);
		}
	}
}

}
