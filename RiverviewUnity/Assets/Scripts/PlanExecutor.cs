using UnityEngine;
using Cloverview;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanExecutor : MonoBehaviour, IDataUser<SaveData>, IDataUser<Nav>, INavigator
{
	[SerializeField]
	string id;

	[SerializeField]
	MenuData executeMenu;

	[SerializeField]
	EventData[] availableEvents;

	[ReadOnly]
	public string parentScene;

	SaveData saveData;
	PlanExecutorSaveData executorSaveData;
	Nav nav;
	Plan plan;
	PlanSchema planSchema;
	string activityScenePreloadId;
	List<PlanActivityData> preloadedActivities = new List<PlanActivityData>();
	List<PlanActivityData> nextPreloadedActivities = new List<PlanActivityData>();
	List<EventData> preloadedEvents = new List<EventData>();

	// NOTE: these are not applied to the character until the plan finishes executing. This allows the game to be saved and reloaded while the plan is being executed, and if the plan data / schema changes the character wont get or miss extra stat changes.
	Character.Status statChangesInProgress;

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.Register<Nav>(this);
	}

	public void OnDisable()
	{
		if (this.nav != null)
		{
			this.UnloadAllPreloadedActivities();
		}
	}

	public void OnApplicationQuit()
	{
		this.nav = null;
	}

	public void Initialise(SaveData saveData)
	{
		Debug.Assert(saveData != null);

		this.saveData = saveData;
		if (this.saveData.planExecutors == null)
		{
			this.saveData.planExecutors = new Dictionary<string, PlanExecutorSaveData>();
		}
		if (!saveData.planExecutors.TryGetValue(this.id, out this.executorSaveData))
		{
			this.executorSaveData = new PlanExecutorSaveData();
			saveData.planExecutors.Add(this.id, this.executorSaveData);
		}

		this.ExecuteIfReady();
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);

		this.nav = nav;

		this.activityScenePreloadId = this.nav.GeneratePreloadIdForEnvScenes();

		this.nav.Preload(this.executeMenu, this.parentScene);

		if (!this.ExecuteIfReady()) { this.PreloadIfReady(); }
	}

	public void Initialise(Plan plan, PlanSchema planSchema)
	{
		this.plan = plan;
		this.planSchema = planSchema;
		if (!this.ExecuteIfReady()) { this.PreloadIfReady(); }
	}

	bool PreloadIfReady()
	{
		if (this.DataReady())
		{
			this.PreloadPlanActivities();
			return true;
		}
		return false;
	}

	bool ExecuteIfReady()
	{
		if (this.DataReady() && this.isActiveAndEnabled && this.executorSaveData.timeUnitsElapsed > 0)
		{
			this.Execute(this.executorSaveData.timeUnitsElapsed);
			return true;
		}
		return false;
	}

	bool DataReady()
	{
		return this.plan != null && this.planSchema != null && this.nav != null && this.saveData != null;
	}

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}

	public void OnOptionSelected(PlanOption selectedOption)
	{
		this.PreloadPlanActivities();
	}

	public void OnOptionDeselected(PlanOption selectedOption)
	{
		this.PreloadPlanActivities();
	}

	void PreloadPlanActivities()
	{
		if (this.plan != null)
		{
			this.nextPreloadedActivities.Clear();

			for (int sectionIndex = 0; sectionIndex < this.plan.sections.Length; ++sectionIndex)
			{
				PlanSection section = this.plan.sections[sectionIndex];
				for (int slotIndex = 0; slotIndex < section.slots.Length; ++slotIndex)
				{
					PlanSlot slot = section.slots[slotIndex];
					if (slot.selectedOption != null)
					{
						PlanActivityData activity = slot.selectedOption.plannerItem.activity;
						if (!this.nextPreloadedActivities.Contains(activity))
						{
							this.nextPreloadedActivities.Add(activity);
						}
					}
				}
			}
			// Remove preload requests for scenes that are no longer in the plan, and add requests for scenes that now are
			for (int activityIndex = 0; activityIndex < this.preloadedActivities.Count; ++activityIndex)
			{
				PlanActivityData activity = this.preloadedActivities[activityIndex];
				if (!this.nextPreloadedActivities.Contains(activity))
				{
					this.nav.RemovePreloadRequest(activity.scene, this.activityScenePreloadId);
				}
			}
			for (int activityIndex = 0; activityIndex < this.nextPreloadedActivities.Count; ++activityIndex)
			{
				PlanActivityData activity = this.nextPreloadedActivities[activityIndex];
				if (!this.preloadedActivities.Contains(activity))
				{
					this.nav.Preload(activity.scene, this.activityScenePreloadId);
				}
			}
			var temp = this.preloadedActivities;
			this.preloadedActivities = this.nextPreloadedActivities;
			this.nextPreloadedActivities = temp;

			this.nextPreloadedActivities.Clear();
		}
		else
		{
			this.UnloadAllPreloadedActivities();
		}
	}

	void UnloadAllPreloadedActivities()
	{
		for (int i = 0; i < this.preloadedActivities.Count; ++i)
		{
			PlanActivityData activity = this.preloadedActivities[i];
			this.nav.RemovePreloadRequest(activity.scene, this.activityScenePreloadId);
		}
	}

	public void Execute(int instantlyExecuteTimeUnits = 0)
	{
		Object.DontDestroyOnLoad(this.gameObject);
		
		this.nav.GoTo(this.executeMenu, this.parentScene);
		this.PreloadPlanActivities();

		this.StartCoroutine(this.ExecuteCoroutine(instantlyExecuteTimeUnits));
	}

	IEnumerator ExecuteCoroutine(int instantlyExecuteTimeUnits)
	{
		Character pc = this.saveData.pc;

		int localTimeUnitsElapsed = 0;
		for (int sectionIndex = 0; sectionIndex < this.planSchema.sections.Length; ++sectionIndex)
		{
			PlanSchemaSection schemaSection = this.planSchema.sections[sectionIndex];
			PlanSection planSection = this.plan.sections[sectionIndex];
			for (int slotIndex = 0; slotIndex < schemaSection.slots.Length; ++slotIndex)
			{
				PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];
				PlanSlot slot = planSection.slots[slotIndex];

				// NOTE(elliot): Slot must be completely covered by the instantly-execute range if it is to be instantly executed (because being completely covered indicates that, if the schema hasn't changed, that slot was finished when this save file was created)
				bool instantSlot = localTimeUnitsElapsed + schemaSlot.unitLength < instantlyExecuteTimeUnits;

				IEnumerator op = this.ExecuteActivity(pc, slot, schemaSlot, instantSlot);
				while (op.MoveNext())
				{
					if (!instantSlot)
					{
						yield return op.Current;
					}
				}

				localTimeUnitsElapsed += schemaSlot.unitLength;
				if (localTimeUnitsElapsed > instantlyExecuteTimeUnits)
				{
					this.executorSaveData.timeUnitsElapsed = localTimeUnitsElapsed;
					yield return 0;
				}
			}
		}

		// TODO(elliot): write final stats out to all characters

		this.executorSaveData.timeUnitsElapsed = 0;

		Debug.Log("Plan done.");
	}

	IEnumerator ExecuteActivity(Character pc, PlanSlot slot, PlanSchemaSlot schemaSlot, bool instant)
	{
		float secondsPerUnitTime = 2;
		if (slot.selectedOption != null)
		{
			int slotLengthTimeUnits = schemaSlot.unitLength;

			PlanOption option = slot.selectedOption;
			Debug.Assert(option.plannerItem != null);
			if (option.plannerItem != null)
			{
				PlanActivityData activity = option.plannerItem.activity;
				var activeActivity = new ActiveActivity()
				{
					def = activity,
					pc = pc
				};

				Debug.LogFormat("Executing plannerItem: {0}", option.plannerItem);

				if (!instant)
				{
					this.nav.GoToActivity(activeActivity, this.activityScenePreloadId);
				}

				while (activeActivity.timeUnitsSpent < slotLengthTimeUnits)
				{
					this.statChangesInProgress = activeActivity.Progress(this.statChangesInProgress);
					activeActivity.timeUnitsSpent += 1;
					if (!instant)
					{
						yield return new WaitForSeconds(secondsPerUnitTime);
					}
				}
			}
		}
	}
}

}
