using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

// UI script for displaying a plan.
public class PlanUI : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
{
	[SerializeField]
	[UnityEngine.Serialization.FormerlySerializedAs("planName")]
	private string planSchemaName;

	[SerializeField]
	private PlanOptionSelectorUI planOptionSelectorUI;

	[SerializeField]
	private PlanOptionUI defaultFilledSlotPrefab;

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
			if (other != null && other != this && other.planSchemaName == this.planSchemaName)
			{
				Debug.LogErrorFormat("PlanUIs with duplicate planName values: {0}, {1}", this, others[i]);
			}
		}

		// Update uiSections if necessary (avoid doing so if not chagned so the scene doesn't get marked dirty unnecessarily)
		PlanSectionUI[] newPlanSections = this.GetComponentsInChildren<PlanSectionUI>(true);
		System.Array.Sort(newPlanSections, PlanSectionUI.Compare);

		bool changed = false;
		if (this.uiSections == null || this.uiSections.Length != newPlanSections.Length) {
			changed = true;
		} else {
			for (int i = 0; i < newPlanSections.Length; ++i) {
				if (this.uiSections[i] != newPlanSections[i]) {
					changed = true;
					break;
				}
			}
		}
		if (changed) {
			Debug.Log("UI Sections updated for " + this.gameObject.name);
			this.uiSections = newPlanSections;
		}

		// Do the same thing for the plan slots
		for (int i = 0; i < this.uiSections.Length; ++i) {
			this.uiSections[i].GatherSlots();
		}

		// Make sure the schema data matches layout
		PlanUI.RefreshSchema(this);
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

		// TODO(elliot): consider passing a unique name here so that plans can be identified as for a specific planning period
		this.plan = SchemaStuff.CreateBlankPlan(this.planSchema, this.planSchemaName);
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

	public static void GenerateOrRefreshSchema(PlanUI planUi)
	{
	#if UNITY_EDITOR
		if (planUi.planSchema == null || planUi.planSchema.name != planUi.planSchemaName) {
			if (UnityEditor.EditorApplication.isPlaying) {
				Debug.LogWarning("Wont generate new plan schema while playing.");
				return;
			}
			planUi.planSchema = ScriptableObject.CreateInstance<PlanSchema>();

			string basePath = "Assets/Data/Planner/Calendars/PlanSchemas/";
			string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
				basePath + planUi.planSchemaName + ".asset"
			);
 
			UnityEditor.AssetDatabase.CreateAsset(planUi.planSchema, assetPath);
 
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
		}
		planUi.planSchema.name = planUi.planSchemaName;

		PlanUI.RefreshSchema(planUi);
	#endif
	}

	public static void RefreshSchema(PlanUI planUi)
	{
	#if UNITY_EDITOR
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
			for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
			{
				PlanSlotUI slotUI = uiSection.slots[slotIndex];

				var schemaSlot = new PlanSchemaSlot();
				schemaSlot.slotType = slotUI.slotType;
				schemaSlot.start = slotUI.GetStartTime();
				schemaSlot.duration = slotUI.GetDuration();
				schemaSection.slots[slotIndex] = schemaSlot;
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
		for (int i = 0; i <	loadedData.plans.Count; ++i) {
			if (loadedData.plans[i].schema == null) {
				// Schema failed to load
				Debug.LogErrorFormat("Failed to load schema for plan {0}.", loadedData.plans[i]);
				continue;
			}
			if (loadedData.plans[i].schema.name == this.planSchema.name) {
				loadedPlan = loadedData.plans[i];
				// A new blank plan has already been generated matching the current schema: set the save data's ref to that new plan. The data from loadedPlan will be migrated to this new plan.
				loadedData.plans[i] = this.plan;
				break;
			}
		}

		if (loadedPlan != null) {
			SchemaStuff.UpgradePlan(this.planSchema, this.plan, loadedPlan);
		} else {
			// This plan hasn't been saved before: add it to the save data
			loadedData.plans.Add(this.plan);
		}
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
			this.planOptionSelectorUI.Populate(this, slot);
			this.planUiAnimator.SetBool("options_open", true);
		}
		else
		{
			if (this.planExecutor != null)
			{
				this.planExecutor.OnOptionDeselected(slot.dataSlot.selectedOption);
			}
			this.selectedSlot.Clear();
			//this.planOptionSelectorUI.Populate(this, slot);
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
