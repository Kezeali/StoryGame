using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanUI : MonoBehaviour, IDataUser<SaveData>
{
	[SerializeField]
	private string planName;

	[SerializeField]
	private PlanOptionSelectorUI planOptionSelectorUI;

	[SerializeField]
	private PlanOptionUI defaultFilledSlotPrefab;

	[SerializeField]
	private PlanExecutor planExecutor;

	[SerializeField]
	private Animator planUiAnimator;

	[ReadOnly]
	public PlanSchema planSchema;

	[ReadOnly]
	[SerializeField]
	private PlanSectionUI[] uiSections;

	private Plan plan;
	private List<GameObject> optionUIs = new List<GameObject>();

	private PlanSlotUI selectedSlot;

	public void OnEnable()
	{
		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>();
		System.Array.Sort(uiSections, PlanSectionUI.Compare);

		for (int sectionIndex = 0; sectionIndex < this.uiSections.Length; ++sectionIndex)
		{
			PlanSectionUI uiSection = this.uiSections[sectionIndex];
			uiSection.GatherSlots();

			int slotsCount = uiSection.slots.Length;
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUi = uiSection.slots[slotIndex];
				slotUi.clicked += HandleSlotClicked;
			}
		}

		this.GenerateSchema();

		App.Register<SaveData>(this);
	}

	public void OnDisable()
	{
		for (int sectionIndex = 0; sectionIndex < this.uiSections.Length; ++sectionIndex)
		{
			PlanSectionUI uiSection = this.uiSections[sectionIndex];
			int slotsCount = uiSection.slots.Length;
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUi = uiSection.slots[slotIndex];
				slotUi.clicked -= HandleSlotClicked;
			}
		}

		this.plan = null;
		this.planSchema = null;
	}

	void GenerateSchema()
	{
		this.planSchema = new PlanSchema();
		this.planSchema.name = planName;

		// NOTE(elliot): A plan that fits the schema is also generated. After plans are loaded from save data, they are transferred into this plan, attempting to migrate the loaded data to the current schema
		this.plan = new Plan();
		this.plan.name = planName;

		int sectionsCount = this.uiSections.Length;

		this.planSchema.sections = new PlanSchemaSection[sectionsCount];
		for (int newSectionIndex = 0; newSectionIndex < sectionsCount; ++newSectionIndex)
		{
			PlanSectionUI uiSection = uiSections[newSectionIndex];

			var schemaSection = new PlanSchemaSection();
			schemaSection.totalTimeUnits = uiSection.TotalTimeUnits();
			this.planSchema.sections[newSectionIndex] = schemaSection;

			int slotsCount = uiSection.slots.Length;

			schemaSection.slots = new PlanSchemaSlot[slotsCount];
			for (int slotIndex = 0; slotIndex < slotsCount-1; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];
				PlanSlotUI nextSlotUI = uiSection.slots[slotIndex+1];

				var schemaSlot = new PlanSchemaSlot();
				schemaSlot.unitIndex = slotUI.SlotUnitIndex();
				schemaSlot.unitLength = nextSlotUI.SlotUnitIndex() - schemaSlot.unitIndex;
				schemaSection.slots[slotIndex] = schemaSlot;
			}
			if (slotsCount > 0)
			{
				PlanSlotUI slotUI = uiSection.slots[slotsCount-1];

				var schemaSlot = new PlanSchemaSlot();
				schemaSlot.unitIndex = slotUI.SlotUnitIndex();
				schemaSlot.unitLength = schemaSection.totalTimeUnits - schemaSlot.unitIndex;
				schemaSection.slots[slotsCount-1] = schemaSlot;
			}
		}

		this.plan.sections = new PlanSection[sectionsCount];
		for (int newSectionIndex = 0; newSectionIndex < sectionsCount; ++newSectionIndex)
		{
			PlanSchemaSection schemaSection = this.planSchema.sections[newSectionIndex];
			PlanSectionUI uiSection = uiSections[newSectionIndex];

			var planSection = new PlanSection();
			this.plan.sections[newSectionIndex] = planSection;

			schemaSection.section = planSection;

			int slotsCount = uiSection.slots.Length;

			planSection.slots = new PlanSlot[slotsCount];
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];

				PlanSlotUI slotUI = uiSection.slots[slotIndex];

				var planSlot = new PlanSlot();
				planSlot.unitIndex = slotUI.SlotUnitIndex();
				planSlot.slotType = slotUI.slotType;
				planSection.slots[slotIndex] = planSlot;

				schemaSlot.slot = planSlot;

				slotUI.Initialise(planSlot);
			}
		}
	}

	public void Initialise(SaveData loadedData)
	{
		if (this.planSchema == null)
		{
			this.GenerateSchema();
		}

		// TODO: get the plan named this.planName from loaded data
		Plan loadedPlan = loadedData.weeklyPlan;
		loadedData.weeklyPlan = this.plan;

		this.selectedSlot = null;

		// Upgrade the data
		int sectionsCount = this.planSchema.sections.Length;
		for (int sectionIndex = 0; sectionIndex < sectionsCount; ++sectionIndex)
		{
			PlanSchemaSection schemaSection = this.planSchema.sections[sectionIndex];
			PlanSection planSection = this.plan.sections[sectionIndex];

			if (loadedPlan != null && loadedPlan.sections.Length > sectionIndex)
			{
				PlanSection loadedSection = loadedPlan.sections[sectionIndex];

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
						PlanSchemaSlot schemaSlot = schemaSection.slots[actualSlotIndex];
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

							// TODO: check that length (unitEnd - unitBegin) < schema unit length
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
		}

		// Populate the slot UI elements
		for (int sectionIndex = 0; sectionIndex < this.uiSections.Length; ++sectionIndex)
		{
			PlanSectionUI uiSection = this.uiSections[sectionIndex];
			int slotsCount = uiSection.slots.Length;
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];
				slotUI.DisplayCurrent(this.defaultFilledSlotPrefab);
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

		this.selectedSlot = null;
		this.planUiAnimator.SetBool("options_open", false);
	}

	public void SelectOption(PlanOptionUI optionUi)
	{
		if (this.selectedSlot != null)
		{
			optionUi.DisableSelection();
			this.selectedSlot.Display(optionUi.planOption, this.defaultFilledSlotPrefab);
			this.selectedSlot = null;
			this.planUiAnimator.SetBool("options_open", false);
		}
	}

	public void HandleSlotClicked(PlanSlotUI slot)
	{
		Debug.Log("HandleSlotClicked(" + slot.ToString() + ")");
		if (this.selectedSlot != slot)
		{
			this.selectedSlot = slot;
			this.planOptionSelectorUI.Populate(slot);
			this.planUiAnimator.SetBool("options_open", true);
		}
		else
		{
			this.planOptionSelectorUI.DeselectOption(slot.dataSlot.selectedOption);
			this.selectedSlot.Clear();
			//this.planOptionSelectorUI.Populate(slot);
		}
	}
}

}
