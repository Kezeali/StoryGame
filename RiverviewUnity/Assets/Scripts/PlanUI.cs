using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanUI : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
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

	[SerializeField]
	public PlanSchema planSchema;

	[ReadOnly]
	[SerializeField]
	private PlanSectionUI[] uiSections;

	Plan plan;
	List<GameObject> optionUIs = new List<GameObject>();

	PlanSlotUI selectedSlot;
	PlanExecutor planExecutor;

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

		this.uiSections = this.GetComponentsInChildren<PlanSectionUI>(true);
		System.Array.Sort(uiSections, PlanSectionUI.Compare);
		PlanUI.GenerateSchema(this);
	#endif
	}

	public void OnEnable()
	{
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

		this.plan = SchemaStuff.CreateBlankPlan(this.planSchema, this.planName);
		this.MapUI();

		App.Register<SaveData>(this);
		App.Register<PlanExecutor>(this);
	}

	public void OnDisable()
	{
		App.Deregister<PlanExecutor>(this);
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

	static void GenerateSchema(PlanUI planUi)
	{
	#if UNITY_EDITOR
		if (planUi.planSchema == null || planUi.planSchema.name != planUi.planName) {
			if (UnityEditor.EditorApplication.isPlaying) {
				Debug.LogWarning("Wont generate new plan schema while playing.");
				return;
			}
			planUi.planSchema = ScriptableObject.CreateInstance<PlanSchema>();

			string basePath = "Assets/Data/Calendars/PlanSchemas/";
			string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
				basePath + planUi.planName + ".asset"
			);
 
			UnityEditor.AssetDatabase.CreateAsset(planUi.planSchema, assetPath);
 
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
		}
		planUi.planSchema.name = planUi.planName;

		int sectionsCount = planUi.uiSections.Length;

		planUi.planSchema.sections = new PlanSchemaSection[sectionsCount];
		for (int newSectionIndex = 0; newSectionIndex < sectionsCount; ++newSectionIndex)
		{
			PlanSectionUI uiSection = planUi.uiSections[newSectionIndex];

			var schemaSection = new PlanSchemaSection();
			schemaSection.totalTimeUnits = uiSection.TotalTimeUnits();
			planUi.planSchema.sections[newSectionIndex] = schemaSection;

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

			UnityEditor.EditorUtility.SetDirty(planUi.planSchema);
		}
	#endif
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

	public void Initialise(SaveData loadedData)
	{
		this.selectedSlot = null;

		Plan loadedPlan = null;
		for (int i = 0; i <	loadedData.plans.Count; ++i)
		{
			if (loadedData.plans[i].name == this.planName)
			{
				loadedPlan = loadedData.plans[i];
				// A new blank plan has already been generated matching the current schema: set the save data's ref to that new plan. The data from loadedPlan will be migrated to this new plan.
				loadedData.plans[i] = this.plan;
				break;
			}
		}

		SchemaStuff.UpgradePlan(this.planSchema, this.plan, loadedPlan);
	}

	public void Initialise(PlanExecutor executor)
	{
		Debug.Assert(executor != null);
		this.planExecutor = executor;
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
		if (this.planExecutor != null && this.planExecutor.IsReadyForPlayerToExecute())
		{
			this.planExecutor.BeginExecution();
		}
	}
}

}
