using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanUI : MonoBehaviour, IServiceUser<Nav>, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
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

	public void OnValidate()
	{
	#if UNITY_EDITOR
		Object[] others = Object.FindObjectsOfType(typeof(PlanUI));
		for (int i = 0; i < others.Length; ++i)
		{
			PlanUI other = others[i] as PlanUI;
			if (other != null && other != this && other.planName == this.planName)
			{
				Debug.LogErrorFormat("PlanUIs with duplicate planName values: {0}, {1}", this, others[i]);
			}
		}
	#endif
	}

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
		this.plan = SchemaStuff.CreateBlankPlan(this.planSchema, this.planName);
		this.MapUI();

		App.Register<Nav>(this);
		App.Register<SaveData>(this);
		App.RegisterPlan(this, this.planName, this.planExecutorPrefab.name);
	}

	public void OnDisable()
	{
		App.DeregisterPlan(this, this.planName, this.planExecutorPrefab.name);
		App.Deregister<Nav>(this);
		App.Deregister<SaveData>(this);

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
				schemaSlot.slotType = slotUI.slotType;
				schemaSlot.unitIndex = slotUI.SlotUnitIndex();
				schemaSlot.unitLength = nextSlotUI.SlotUnitIndex() - schemaSlot.unitIndex;
				schemaSection.slots[slotIndex] = schemaSlot;
			}
			if (slotsCount > 0)
			{
				PlanSlotUI slotUI = uiSection.slots[slotsCount-1];

				var schemaSlot = new PlanSchemaSlot();
				schemaSlot.slotType = slotUI.slotType;
				schemaSlot.unitIndex = slotUI.SlotUnitIndex();
				schemaSlot.unitLength = schemaSection.totalTimeUnits - schemaSlot.unitIndex;
				schemaSection.slots[slotsCount-1] = schemaSlot;
			}
		}
	}

	void MapUI()
	{
		int sectionsCount = this.uiSections.Length;
		for (int sectionIndex = 0; sectionIndex < sectionsCount; ++sectionIndex)
		{
			PlanSectionUI uiSection = this.uiSections[sectionIndex];
			PlanSection planSection = this.plan.sections[sectionIndex];

			int slotsCount = uiSection.slots.Length;
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];
				PlanSlot planSlot = planSection.slots[slotIndex];
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
		this.selectedSlot = null;

		Plan loadedPlan = null;
		for (int i = 0; i <	loadedData.plans.Count; ++i)
		{
			if (loadedData.plans[i].name == this.planName)
			{
				loadedPlan = loadedData.plans[i];
				loadedData.plans[i] = this.plan;
				break;
			}
		}

		SchemaStuff.UpgradePlan(this.planSchema, this.plan, loadedPlan);
	}

	public void Initialise(PlanExecutor executor)
	{
		this.planExecutor = executor;

		this.planExecutor.SetPlan(this.plan, this.planSchema);
	}

	public void CompleteInitialisation()
	{
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

		if (this.planExecutor != null)
		{
			this.planExecutor.SetPlan(this.plan, this.planSchema);
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

			if (this.planExecutor != null)
			{
				this.planExecutor.OnOptionSelected(optionUi.planOption);
			}
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
			if (this.planExecutor != null)
			{
				this.planExecutor.OnOptionDeselected(slot.dataSlot.selectedOption);
			}

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
