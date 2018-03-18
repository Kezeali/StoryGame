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
	MenuData backMenu;

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
	List<EventData> nextPreloadedEvents = new List<EventData>();

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

	void FilterAvailableEvents()
	{
		if (this.saveData != null)
		{
			Character pc = this.saveData.pc;

			this.nextPreloadedEvents.Clear();

			for (int eventDataIndex = 0; eventDataIndex < this.availableEvents.Length; ++eventDataIndex)
			{
				EventData eventDef = this.availableEvents[eventDataIndex];
				Debug.Assert(!this.nextPreloadedEvents.Contains(eventDef));
				EventConditions conditions = eventDef.conditions;
				if (!DesiredStat.DetermineHardNo(pc, conditions.requiredPcStats))
				{
					this.nextPreloadedEvents.Add(eventDef);
				}
			}
			// TODO(elliot): cache the sorter to avoid extra garbage creation
			this.nextPreloadedEvents.Sort(new EventDataSorter() { pc = pc });

			// Remove preload requests for events that are no longer available, and add requests for events that now are
			// for (int eventIndex = 0; eventIndex < this.preloadedEvents.Count; ++eventIndex)
			// {
			// 	EventData eventDef = this.preloadedEvents[eventIndex];
			// 	if (!this.nextPreloadedEvents.Contains(eventDef))
			// 	{
			// 		this.nav.RemovePreloadRequest(eventDef.scene, this.activityScenePreloadId);
			// 	}
			// }
			// for (int eventIndex = 0; eventIndex < this.nextPreloadedEvents.Count; ++eventIndex)
			// {
			// 	EventData eventDef = this.nextPreloadedEvents[eventIndex];
			// 	if (!this.preloadedEvents.Contains(eventDef))
			// 	{
			// 		this.nav.Preload(eventDef.scene, this.activityScenePreloadId);
			// 	}
			// }
			var temp = this.preloadedEvents;
			this.preloadedEvents = this.nextPreloadedEvents;
			this.nextPreloadedEvents = temp;

			this.nextPreloadedEvents.Clear();
		}
		else
		{
			this.UnloadAllPreloadedActivities();
		}
	}

	public class EventDataSorter : IComparer<EventData>
	{
		public Character pc;
		public int Compare(EventData a, EventData b)
		{
			// NOTE(elliot): assumes neither event has any hard-nos
			if (a.conditions.priority < b.conditions.priority)
			{
				return -1;
			}
			else if (a.conditions.priority > b.conditions.priority)
			{
				return 1;
			}
			else
			{
				float ratingA = DesiredStat.Rate(pc.status, a.conditions.requiredPcStats);
				float ratingB = DesiredStat.Rate(pc.status, b.conditions.requiredPcStats);
				if (ratingA > ratingB)
				{
					return -1;
				}
				else if (ratingA < ratingB)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}
	}

	public void Execute(int skipTimeUnits = 0)
	{
		Object.DontDestroyOnLoad(this.gameObject);
		
		this.nav.GoTo(this.executeMenu, this.parentScene);
		this.PreloadPlanActivities();

		this.StartCoroutine(this.ExecuteCoroutine(skipTimeUnits, 0));
	}

	IEnumerator ExecuteCoroutine(int skipTimeUnits, int instantTimeUnits)
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
				bool skipSlot = localTimeUnitsElapsed + schemaSlot.unitLength < skipTimeUnits;

				bool instantSlot = localTimeUnitsElapsed + schemaSlot.unitLength < instantTimeUnits;

				if (!skipSlot)
				{
					// NOTE(elliot): even if the slot itself isn't skipped, skip commute-to events if the skip time is beyond the start of the slot
					bool skipCommuteTo = localTimeUnitsElapsed <= skipTimeUnits;
					if (!skipCommuteTo)
					{
						EventData selectedEvent = this.SelectEventFor(slot, schemaSlot, DesiredSlot.When.Before);
						if (selectedEvent != null)
						{
							ActiveEvent activeEvent = new ActiveEvent()
							{
								def = selectedEvent,
								pc = pc
							};

							PlanActivityData activity = slot.selectedOption.plannerItem.activity;
							this.nav.GoToCommute(activeEvent, this.activityScenePreloadId);

							// while (activeEvent.timeUnitsSpent < slotLengthTimeUnits)
							// {
							// 	this.statChangesInProgress = activeActivity.Progress(this.statChangesInProgress);
							// 	activeActivity.timeUnitsSpent += 1;

							// 	yield return new WaitForSeconds(secondsPerUnitTime);
							// }

							this.nav.FinishCommute(this.activityScenePreloadId);
						}
					}

					IEnumerator op = this.ExecuteActivity(pc, slot, schemaSlot, instantSlot);
					while (op.MoveNext())
					{
						if (!instantSlot)
						{
							yield return op.Current;
						}
					}
				}

				localTimeUnitsElapsed += schemaSlot.unitLength;

				bool skipCommuteFrom = localTimeUnitsElapsed <= skipTimeUnits;
				if (!skipCommuteFrom)
				{
					EventData selectedEvent = this.SelectEventFor(slot, schemaSlot, DesiredSlot.When.After);
					if (selectedEvent != null)
					{
						ActiveEvent activeEvent = new ActiveEvent()
						{
							def = selectedEvent,
							pc = pc
						};

						PlanActivityData activity = slot.selectedOption.plannerItem.activity;
						this.nav.GoToCommute(activeEvent, this.activityScenePreloadId);

						// while (activeEvent.timeUnitsSpent < slotLengthTimeUnits)
						// {
						// 	this.statChangesInProgress = activeActivity.Progress(this.statChangesInProgress);
						// 	activeActivity.timeUnitsSpent += 1;

						// 	yield return new WaitForSeconds(secondsPerUnitTime);
						// }

						this.nav.FinishCommute(this.activityScenePreloadId);
					}
				}

				if (localTimeUnitsElapsed > skipTimeUnits)
				{
					this.executorSaveData.timeUnitsElapsed = localTimeUnitsElapsed;
					yield return 0;
				}
			}
		}

		// TODO(elliot): write final stats out to all characters

		this.executorSaveData.timeUnitsElapsed = 0;

		Debug.Log("Plan done.");

		this.UnloadAllPreloadedActivities();

		this.nav.GoTo(this.backMenu, this.parentScene);
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

	EventData SelectEventFor(PlanSlot slot, PlanSchemaSlot schemaSlot, DesiredSlot.When when)
	{
		this.FilterAvailableEvents();

		Random.state = this.executorSaveData.randomState;

		EventData result = null;
		for (int eventDataIndex = 0; eventDataIndex < this.preloadedEvents.Count; ++eventDataIndex)
		{
			EventData def = this.preloadedEvents[eventDataIndex];
			EventConditions conditions = def.conditions;
			DesiredSlot[] slotConditions = conditions.slotConditions;
		}

		this.executorSaveData.randomState = Random.state;

		return result;
	}
}

}