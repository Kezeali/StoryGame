using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanUI : MonoBehaviour, IServiceUser<Nav>, IServiceUser<SaveData>
{
	[SerializeField]
	private string planName;

	[SerializeField]
	private PlanOptionSelectorUI planOptionSelectorUI;

	[SerializeField]
	private PlanOptionUI defaultFilledSlotPrefab;

	[SerializeField]
	[UnityEngine.Serialization.FormerlySerializedAs("planExecutor")]
	private PlanExecutor planExecutorPrefab;

	[SerializeField]
	private Animator planUiAnimator;

	[ReadOnly]
	public PlanSchema planSchema;

	[ReadOnly]
	[SerializeField]
	private PlanSectionUI[] uiSections;

	Plan plan;
	List<GameObject> optionUIs = new List<GameObject>();

	PlanSlotUI selectedSlot;
	PlanExecutor planExecutor;
	Nav nav;

	public void OnEnable()
	{
		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>(true);
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
		this.CreateBlankPlan();

		App.Register<Nav>(this);
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
	}

	void GenerateSchema()
	{
		this.planSchema = new PlanSchema();
		this.planSchema.name = planName;

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
	}

	void CreateBlankPlan()
	{
		int sectionsCount = this.uiSections.Length;

		this.plan = new Plan();
		this.plan.name = planName;

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

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav != null);

		this.nav = nav;
	}

	public void Initialise(SaveData loadedData)
	{
		this.plan.ClearSelections();

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

					if (loadedSlot.selectedOption == null)
					{
						continue;
					}

					int loadedSlotUnitBegin = loadedSlot.unitIndex;
					int selectedOptionLength = 0;
					if (loadedSlot.selectedOption != null && loadedSlot.selectedOption.plannerItem != null)
					{
						selectedOptionLength = loadedSlot.selectedOption.plannerItem.timeUnits;
					}
					// this is just a guess, but that's fine
					// TODO(elliot): consider saving the schema for each plan so this doesn't have to be guessed?
					int loadedSlotUnitEnd = loadedSlotUnitBegin + selectedOptionLength;

					SlotType requiredType = loadedSlot.slotType;

					// Look for the first empty & valid slot that overlaps the given entry
					for (int actualSlotIndex = 0; actualSlotIndex < planSection.slots.Length; ++ actualSlotIndex)
					{
						PlanSchemaSlot schemaSlot = schemaSection.slots[actualSlotIndex];
						PlanSlot actualSlot = planSection.slots[actualSlotIndex];

						if (actualSlot.slotType == requiredType && actualSlot.selectedOption == null)
						{
							int actualUnitLength = schemaSlot.unitLength;
							int actualBegin = schemaSlot.unitIndex;
							int actualEnd = actualBegin + actualUnitLength;

							// NOTE(elliot): this should check whether both, 1) the loaded slot overlaps the slot currently being checked (called "actualSlot", as it is a slot that is actually in the current schema), and 2) the selected option in the loaded slot will fit in the actual slot
							if (((loadedSlotUnitBegin > actualBegin && loadedSlotUnitBegin <= actualEnd) || (loadedSlotUnitEnd > actualBegin && loadedSlotUnitEnd <= actualEnd))
								&& selectedOptionLength <= actualUnitLength)
							{
								actualSlot.selectedOption = loadedSlot.selectedOption;
								break;
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

		if (this.planExecutor == null)
		{
			this.nav.MakeCurrentMenuTheActiveScene();
			this.planExecutor = Object.Instantiate(this.planExecutorPrefab);
		}
		if (this.planExecutor != null)
		{
			this.planExecutor.Initialise(this.plan, this.planSchema);
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

	public void Execute()
	{
		if (this.planExecutor != null)
		{
			this.planExecutor.Execute();
			this.planExecutor = null;
		}
	}
}

}
