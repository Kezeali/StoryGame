using UnityEngine;
using Cloverview;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanExecutor : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<Nav>, INavigator
{
	[SerializeField]
	float defaultSecondsPerUnitTime;

	[SerializeField]
	MenuData executeMenu;

	[SerializeField]
	[UnityEngine.Serialization.FormerlySerializedAs("backMenu")]
	MenuData defaultBackMenu;

	[SerializeField]
	EventData[] availableEvents;

	[ReadOnly]
	public string parentScene;

	[System.NonSerialized]
	public string instantiatedFrom;

	[System.NonSerialized]
	public string expectedPlanName;

	[System.NonSerialized]
	public float executingSecondsPerUnitTime;

	[System.NonSerialized]
	public string currentActivityName;

	[System.NonSerialized]
	public string key;

	[System.NonSerialized]
	public IPlanExecutorController controller;

	SaveData saveData;
	PlanExecutorSaveData executorSaveData;
	Nav nav;
	Plan plan;
	PlanSchema planSchema;
	string activityScenePreloadId;
	List<PlanActivityData> preloadedActivities = new List<PlanActivityData>();
	List<PlanActivityData> nextPreloadedActivities = new List<PlanActivityData>();
	List<EventData> filteredEvents = new List<EventData>();
	List<EventData> nextFilteredEvents = new List<EventData>();

	bool executing;
	List<PlanExecutor> others = new List<PlanExecutor>(5);

	public const float MIN_SECONDS_PER_UNIT_TIME =  (1.0f / 120.0f);

	public void Reset()
	{
		this.defaultSecondsPerUnitTime = 2;
	}

	public void OnEnable()
	{
		this.RefreshOthers();
		App.Register<SaveData>(this);
		App.Register<Nav>(this);
	}

	public void OnDisable()
	{
		App.Deregister<SaveData>(this);
		App.Deregister<Nav>(this);

		this.others.Clear();
		if (this.nav != null) {
			this.UnloadAllPreloadedActivities();
		}
	}

	public void OnApplicationQuit()
	{
		this.nav = null;
	}

	void RefreshOthers()
	{
		this.others.Clear();
		GameObject[] existingExecutors = GameObject.FindGameObjectsWithTag("PlanExecutor");
		for (int i = 0; i < existingExecutors.Length; ++i) {
			if (existingExecutors[i] != this.gameObject) {
				PlanExecutor otherExecutor = existingExecutors[i].GetComponent<PlanExecutor>();
				if (otherExecutor != null) {
					this.others.Add(otherExecutor);
				} else {
					Debug.LogErrorFormat("Non PlanExecutor tagged with PlanExecutor tag: {0}", existingExecutors[i].name);
				}
			}
		}
	}

	public void SetKey(string instantiatedFrom, string expectedPlanName)
	{
		this.instantiatedFrom = instantiatedFrom;
		this.expectedPlanName = expectedPlanName;
		this.key = Strf.Format("{0}({1})", this.instantiatedFrom, this.expectedPlanName);
		Debug.LogFormat("Assigned executor key '{0}'", this.key);
		this.LoadSave();
	}

	public void Initialise(SaveData saveData)
	{
		Debug.Assert(saveData != null);
		this.saveData = saveData;
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);

		this.nav = nav;
	}

	public void CompleteInitialisation()
	{
		this.activityScenePreloadId = this.nav.GeneratePreloadIdForEnvScenes();

		if (!string.IsNullOrEmpty(this.parentScene)) {
			this.nav.Preload(this.executeMenu, this.parentScene);
		}
		
		this.LoadSave();
		if (!this.ResumeExecutionIfReady()) { this.PreloadIfReady(); }
	}

	void LoadSave()
	{
		// TODO: abort execution whenever service initialisation happens
		Debug.Assert(!this.executing);

		if (this.saveData != null && !string.IsNullOrEmpty(this.key)) {
			Debug.LogFormat("Loading save data for executor key '{0}'", this.key);
			if (this.saveData.planExecutors == null) {
				this.saveData.planExecutors = new Dictionary<string, PlanExecutorSaveData>();
			}
			if (!saveData.planExecutors.TryGetValue(this.key, out this.executorSaveData)) {
				this.executorSaveData = new PlanExecutorSaveData();
				saveData.planExecutors.Add(this.key, this.executorSaveData);
			}

			if (this.executorSaveData != null) {
				// Resuming: try to load the plan in the state execution was saved in
				if (this.executorSaveData.timeUnitsElapsed > 0 && this.executorSaveData.livePlan != null && this.executorSaveData.liveSchema != null) {
					this.planSchema = this.executorSaveData.liveSchema;
					this.plan = this.executorSaveData.livePlan;
					// TODO(elliot): Make sure the loaded plan actually matches the loaded schema
					// NOTE(elliot): don't have to worry about PlanUI upgrading the loaded plan to a new, conflicting schema, even if livePlan points to the same object as the PlanUI plan in the saveData, as PlanUI creates a new plan and upgrades the data from loaded object into that
				} else {
					// If any of the values required to resume were missing, reset them all
					this.executorSaveData.livePlan = null;
					this.executorSaveData.liveSchema = null;
					this.executorSaveData.timeUnitsElapsed = 0;
				}
			}
		}
	}

	public void SetPlan(Plan plan, PlanSchema planSchema)
	{
		if (!this.executing && !this.IsReadyToResume()) {
			this.plan = plan;
			this.planSchema = planSchema;

			this.PreloadIfReady();
		}
	}

	bool IsReadyToResume()
	{
		return this.executorSaveData != null && this.executorSaveData.timeUnitsElapsed > 0 && this.executorSaveData.livePlan != null && this.executorSaveData.liveSchema != null;
	}

	public string GetExpectedPlanName()
	{
		return this.expectedPlanName;
	}

	bool PreloadIfReady()
	{
		if (this.DataReadyToBeginExecution()) {
			this.PreloadPlanActivities();
			return true;
		}
		return false;
	}

	bool OthersExecuting()
	{
		bool result = false;
		for (int i = 0; i < this.others.Count; ++i) {
			if (this.others[i] != null && this.others[i].executing) {
				result = true;
				break;
			}
		}
		return result;
	}

	string FindOtherExecuting()
	{
		string result = "";
		for (int i = 0; i < this.others.Count; ++i) {
			if (this.others[i] != null && this.others[i].executing) {
				result = this.others[i].instantiatedFrom;
				break;
			}
		}
		return result;
	}
 
	bool ResumeExecutionIfReady()
	{
		if (this.RequiredServicesAreInitialised() &&
			this.executorSaveData.timeUnitsElapsed > 0 &&
			this.isActiveAndEnabled &&
			!this.OthersExecuting())
		{
			this.Execute(this.executorSaveData.timeUnitsElapsed);
			return true;
		}
		return false;
	}

	bool RequiredServicesAreInitialised()
	{
		return this.nav != null && 
			this.saveData != null && 
			this.executorSaveData != null;
	}

	bool DataReadyToBeginExecution()
	{
		return
			this.plan != null && 
			this.planSchema != null;
	}

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}

	public void OnOptionSelected(PlanOption selectedOption)
	{
		if (this.DataReadyToBeginExecution()) {
			this.PreloadPlanActivities();
		}
	}

	public void OnOptionDeselected(PlanOption selectedOption)
	{
		if (this.DataReadyToBeginExecution()) {
			this.PreloadPlanActivities();
		}
	}

	void PreloadPlanActivities()
	{
		if (this.plan != null) {
			this.nextPreloadedActivities.Clear();

			for (int sectionIndex = 0; sectionIndex < this.plan.sections.Length; ++sectionIndex) {
				PlanSection section = this.plan.sections[sectionIndex];
				PlanSlot previousSlot = null;
				for (int slotIndex = 0; slotIndex < section.slots.Length; ++slotIndex) {
					PlanSlot slot = section.slots[slotIndex];
					if (slot.selectedOption != null) {
						PlanActivityData activity = slot.selectedOption.plannerItem.activity;
						if (!this.nextPreloadedActivities.Contains(activity)) {
							this.nextPreloadedActivities.Add(activity);
						}

						if (previousSlot != null) {
							// TODO(elliot): preload commute scene from previous slot to this slot
							//this.nextPreloadedCommutes.Add(new ExpectedCommute(from, to));
						} else {
							// TODO(elliot): preload commute from home to first slot
						}
					}
					previousSlot = slot;
				}
				if (previousSlot != null) {
					// TODO(elliot): Preload commute from final slot to home
				}
			}
			// Remove preload requests for scenes that are no longer in the plan, and add requests for scenes that now are
			for (int activityIndex = 0; activityIndex < this.preloadedActivities.Count; ++activityIndex) {
				PlanActivityData activity = this.preloadedActivities[activityIndex];
				if (!this.nextPreloadedActivities.Contains(activity)) {
					this.nav.RemovePreloadRequest(activity.scene, this.activityScenePreloadId);
				}
			}
			for (int activityIndex = 0; activityIndex < this.nextPreloadedActivities.Count; ++activityIndex) {
				PlanActivityData activity = this.nextPreloadedActivities[activityIndex];
				if (!this.preloadedActivities.Contains(activity)) {
					this.nav.Preload(activity.scene, this.activityScenePreloadId);
				}
			}
			var temp = this.preloadedActivities;
			this.preloadedActivities = this.nextPreloadedActivities;
			this.nextPreloadedActivities = temp;

			this.nextPreloadedActivities.Clear();
		} else {
			this.UnloadAllPreloadedActivities();
		}
	}

	void UnloadAllPreloadedActivities()
	{
		for (int i = 0; i < this.preloadedActivities.Count; ++i) {
			PlanActivityData activity = this.preloadedActivities[i];
			this.nav.RemovePreloadRequest(activity.scene, this.activityScenePreloadId);
		}
	}

	EventDataSorter sorter = new EventDataSorter();
	void FilterAvailableEvents()
	{
		if (this.saveData != null) {
			Character pc = this.saveData.pc;

			this.nextFilteredEvents.Clear();

			for (int eventDataIndex = 0; eventDataIndex < this.availableEvents.Length; ++eventDataIndex) {
				EventData eventDef = this.availableEvents[eventDataIndex];
				Debug.Assert(!this.nextFilteredEvents.Contains(eventDef));
				EventConditions conditions = eventDef.conditions;
				if (conditions.requiredPcStats == null || !DesiredStat.DetermineHardNo(pc, conditions.requiredPcStats)) {
					this.nextFilteredEvents.Add(eventDef);
				}
			}

			sorter.pc = pc;
			this.nextFilteredEvents.Sort(sorter);

			var temp = this.filteredEvents;
			this.filteredEvents = this.nextFilteredEvents;
			this.nextFilteredEvents = temp;

			this.nextFilteredEvents.Clear();
		}
	}

	public class EventDataSorter : IComparer<EventData>
	{
		public Character pc;
		public int Compare(EventData a, EventData b)
		{
			if (a.conditions.priority < b.conditions.priority) {
				return -1;
			} else if (a.conditions.priority > b.conditions.priority) {
				return 1;
			} else {
				float ratingA = DesiredStat.Rate(pc.status, a.conditions.requiredPcStats);
				float ratingB = DesiredStat.Rate(pc.status, b.conditions.requiredPcStats);
				if (ratingA > ratingB) {
					return -1;
				} else if (ratingA < ratingB) {
					return 1;
				} else {
					return 0;
				}
			}
		}
	}

	public bool IsReadyForPlayerToExecute()
	{
		return !this.OthersExecuting() && this.DataReadyToBeginExecution();
	}

	public void Execute(int skipTimeUnits = 0)
	{
		if (!this.executing) {
			this.gameObject.SetActive(true);
			if (!this.OthersExecuting()) {
				this.StartCoroutine(this.ExecuteCoroutine(this.defaultSecondsPerUnitTime, skipTimeUnits, 0));
			} else {
				string otherExecutorName = this.FindOtherExecuting();
				Debug.LogWarningFormat("Tried to execute while another executor ({0}) executing!", otherExecutorName);
			}
		} else {
			Debug.LogWarningFormat("Tried to execute again while {0} already executing!", this.instantiatedFrom);
		}
	}

	IEnumerator ExecuteCoroutine(float secondsPerUnitTime, int skipTimeUnits, int instantTimeUnits)
	{
		this.executing = true;

		bool success = App.ExecutorBeginning(this);
		if (!success) {
			this.executing = false;
			yield break;
		}

		// Save the plan name and schema in use
		this.executorSaveData.livePlan = this.plan;
		this.executorSaveData.liveSchema = this.planSchema;

		if (this.executorSaveData.backMenu == null) {
			// Save the menu to return to
			Nav.VisibleMenu sourceMenu = this.nav.nextActiveMenu ?? this.nav.activeMenu;
			if (sourceMenu != null && sourceMenu.def != null) {
				this.executorSaveData.backMenu = this.nav.activeMenu.def;
			} else {
				this.executorSaveData.backMenu = this.defaultBackMenu;
			}
		}

		if (!string.IsNullOrEmpty(this.parentScene)) {
			this.nav.GoTo(this.executeMenu, this.parentScene);
		} else {
			this.nav.GoTo(this.executeMenu);
		}
		this.PreloadPlanActivities();

		while (this.nav.IsProcessingGoToMenuQueue()) {
			yield return 0;
		}

		this.executingSecondsPerUnitTime = secondsPerUnitTime;

		Cast liveCast = null;
		if (skipTimeUnits > 0) {
			liveCast = this.executorSaveData.liveCast;
		}
		if (liveCast == null) {
			liveCast = new Cast();
		}
		if (liveCast.pc == null) {
			liveCast.pc = this.saveData.pc.CreateSimulationClone();
		}
		if (liveCast.leadNpcs == null) {
			liveCast.leadNpcs = new List<Character>(this.saveData.leadNpcs.Count);
			for (int npcIndex = 0; npcIndex < this.saveData.leadNpcs.Count; ++npcIndex) {
				Character realNpc = this.saveData.leadNpcs[npcIndex];
				liveCast.leadNpcs.Add(realNpc.CreateSimulationClone());
			}
		}
		this.executorSaveData.liveCast = liveCast;

		if (skipTimeUnits > 0) {
			Random.state = this.executorSaveData.randomState;
		} else {
			Random.InitState(1234818);
			this.executorSaveData.randomState = Random.state;
		}

		int localTimeUnitsElapsed = 0;
		for (int sectionIndex = 0; sectionIndex < this.planSchema.sections.Length; ++sectionIndex) {
			PlanSchemaSection schemaSection = this.planSchema.sections[sectionIndex];
			PlanSection planSection = this.plan.sections[sectionIndex];
			for (int slotIndex = 0; slotIndex < schemaSection.slots.Length; ++slotIndex) {
				PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];
				PlanSlot slot = planSection.slots[slotIndex];

				// NOTE(elliot): Slot must be completely covered by the instantly-execute range if it is to be instantly executed (because being completely covered indicates that, if the schema hasn't changed, that slot was finished when this save file was created)
				bool skipSlot = localTimeUnitsElapsed + schemaSlot.unitLength < skipTimeUnits;

				bool instantSlot = localTimeUnitsElapsed + schemaSlot.unitLength < instantTimeUnits;

				if (!skipSlot) {
					// NOTE(elliot): even if the slot itself isn't skipped, skip commute-to events if the skip time is beyond the start of the slot
					bool skipCommuteTo = localTimeUnitsElapsed <= skipTimeUnits;
					if (!skipCommuteTo) {
						SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.Before);
						if (selectedEvent.def != null) {
							Debug.LogFormat("Executing event {0}", selectedEvent.def);

							ActiveEvent activeEvent = new ActiveEvent()
							{
								def = selectedEvent.def,
								cast = liveCast
							};

							SceneData scene = null;
							PlanActivityData activityData = GetActivity(slot);
							if (activityData != null) {
								scene = activityData.scene;
							}

							this.nav.GoToCommute(activeEvent, scene, this.activityScenePreloadId);
							yield return 0;

							EventProgressResult result = EventProgressResult.Continue;
							while (result == EventProgressResult.Continue) {
								result = activeEvent.Progress();
								yield return 0;
							}

							this.nav.FinishCommute(this.activityScenePreloadId);

							// The event has completed, push status back to the primary save data
							PlanExecutor.ApplyNewStatuses(this.saveData, liveCast);
						}
					}

					IEnumerator op = this.ExecuteActivity(liveCast, slot, schemaSlot, this.saveData.time + localTimeUnitsElapsed, instantSlot ? 0 : secondsPerUnitTime);
					while (op.MoveNext()) {
						if (!instantSlot) {
							yield return op.Current;
						}
					}

					// The activty has completed, push status back to the primary save data
					PlanExecutor.ApplyNewStatuses(this.saveData, liveCast);
				}

				localTimeUnitsElapsed += schemaSlot.unitLength;

				if (localTimeUnitsElapsed > skipTimeUnits) {
					this.executorSaveData.timeUnitsElapsed = localTimeUnitsElapsed;
					yield return 0;
				}

				bool skipCommuteFrom = localTimeUnitsElapsed <= skipTimeUnits;
				if (!skipCommuteFrom) {
					SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.After);
					if (selectedEvent.def != null) {
						Debug.LogFormat("Executing event {0}", selectedEvent.def);

						ActiveEvent activeEvent = new ActiveEvent()
						{
							def = selectedEvent.def,
							cast = liveCast
						};

						SceneData scene = null;
						if (slotIndex < schemaSection.slots.Length-1) {
							PlanSlot nextSlot = planSection.slots[slotIndex+1];
							PlanActivityData activityData = GetActivity(nextSlot);
							if (activityData != null) {
								scene = activityData.scene;
							}
						} else {
							PlanActivityData activityData = GetActivity(slot);
							if (activityData != null) {
								scene = activityData.scene;
							}
						}

						this.nav.GoToCommute(activeEvent, scene, this.activityScenePreloadId);
						yield return 0;

						EventProgressResult result = EventProgressResult.Continue;
						while (result == EventProgressResult.Continue) {
							result = activeEvent.Progress();
							yield return 0;
						}

						this.nav.FinishCommute(this.activityScenePreloadId);

						// The event has completed, push status back to the primary save data
						PlanExecutor.ApplyNewStatuses(this.saveData, liveCast);
					}
				}
			}
		}

		PlanExecutor.ApplyNewStatuses(this.saveData, liveCast);

		this.saveData.time += this.executorSaveData.timeUnitsElapsed;


		this.executorSaveData.livePlan = null;
		this.executorSaveData.liveSchema = null;
		this.executorSaveData.liveCast = null;
		this.executorSaveData.timeUnitsElapsed = 0;
		// Copy the value before resetting it so it can be used to actually go back!
		MenuData nextMenu = this.executorSaveData.backMenu;
		this.executorSaveData.backMenu = null;

		Debug.Log("Plan done.");

		this.executing = false;
		App.ExecutorEnding(this);

		this.UnloadAllPreloadedActivities();
                 
		// Allow the controller to pass in a new plan in case it has changed from the live-plan that just finished executing
		if (this.controller != null) {
			this.controller.ReceiveExecutor(this);
		}

		this.nav.GoTo(nextMenu, this.parentScene);
	}

	static void ApplyNewStatuses(SaveData saveData, Cast liveCast)
	{
		saveData.pc.ApplyStatus(liveCast.pc);
		for (int simNpcIndex = 0; simNpcIndex < liveCast.leadNpcs.Count; ++simNpcIndex) {
			Character simNpc = liveCast.leadNpcs[simNpcIndex];
			Character actualNpc = null;
			for (int npcIndex = 0; npcIndex < saveData.leadNpcs.Count; ++npcIndex) {
				if (saveData.leadNpcs[npcIndex].name == simNpc.name) {
					actualNpc = saveData.leadNpcs[npcIndex];
				}
			}
			if (actualNpc != null) {
				actualNpc.ApplyStatus(simNpc);
			} else {
				Debug.LogWarningFormat("NPC has gone missing: expected NPC with name '{0}'", simNpc.name);
			}
		}
	}

	static PlanActivityData GetActivity(PlanSlot slot)
	{
		PlanActivityData result = null;
		if (slot.selectedOption != null && slot.selectedOption.plannerItem != null) {
			result = slot.selectedOption.plannerItem.activity;
		}
		return result;
	}

	IEnumerator ExecuteActivity(Cast liveCast, PlanSlot slot, PlanSchemaSlot schemaSlot, int beginTimeUnit, float secondsPerUnitTime)
	{
		bool instant = secondsPerUnitTime < MIN_SECONDS_PER_UNIT_TIME;
		if (slot.selectedOption != null) {
			int slotLengthTimeUnits = schemaSlot.unitLength;
			int timeUnitsBeforeEvent = slotLengthTimeUnits / 2;

			SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.During);

			// NOTE(elliot): if a specific time was specified, rather than a slot type, this attempts to make the event occur at that time in the execution
			if (selectedEvent.def != null) {
				if (selectedEvent.triggeredCondition.time >= 0) {
					int desiredTimeUnitsBeforeEvent = selectedEvent.triggeredCondition.time - beginTimeUnit;
					int maxTimeUnitsBeforeEvent = slotLengthTimeUnits > 0 ? slotLengthTimeUnits-1 : 0;

					timeUnitsBeforeEvent = Mathf.Clamp(desiredTimeUnitsBeforeEvent, 0, maxTimeUnitsBeforeEvent);
				}
			}

			PlanOption option = slot.selectedOption;
			Debug.Assert(option.plannerItem != null);
			if (option.plannerItem != null) {
				PlanActivityData activity = option.plannerItem.activity;
				var activeActivity = new ActiveActivity()
				{
					def = activity,
					subjectDef = option.plannerItem.subject,
					cast = liveCast,
					beginTimeUnit = beginTimeUnit
				};

				Debug.LogFormat("Executing plannerItem: {0}", option.plannerItem);

				this.currentActivityName = option.plannerItem.name;

				if (!instant) {
					this.nav.GoToActivity(activeActivity, this.activityScenePreloadId);
					yield return 0;
				}

				while (activeActivity.timeUnitsSpent < timeUnitsBeforeEvent) {
					activeActivity.Progress();
					activeActivity.timeUnitsSpent += 1;
					if (!instant) {
						yield return new WaitForSeconds(secondsPerUnitTime);
					}
				}

				if (selectedEvent.def != null) {
					Debug.LogFormat("Executing event {0}", selectedEvent.def);

					ActiveEvent activeEvent = new ActiveEvent()
					{
						def = selectedEvent.def,
						cast = liveCast
					};

					SceneData activityScene = null;
					if (!instant) {
						activityScene = activity.scene;
					}
					// NOTE(elliot): this is just to make sure the activity scene is passed to the event as soon as it loads (if it hasn't already). no actual commute is expected, as the activity scene should already be the current scene.
					this.nav.GoToCommute(activeEvent, activityScene, this.activityScenePreloadId);
					yield return 0;

					EventProgressResult result = EventProgressResult.Continue;
					while (result == EventProgressResult.Continue) {
						result = activeEvent.Progress();
						yield return 0;
					}

					this.nav.FinishCommute(this.activityScenePreloadId);
				}

				while (activeActivity.timeUnitsSpent < slotLengthTimeUnits) {
					activeActivity.Progress();
					activeActivity.timeUnitsSpent += 1;

					liveCast.pc.UpdateStatBonuses(beginTimeUnit + activeActivity.timeUnitsSpent);

					if (!instant) {
						yield return new WaitForSeconds(secondsPerUnitTime);
					}
				}

				activeActivity.Finish();
			}
		}
	}

	struct SelectedEvent
	{
		public EventData def;
		public DesiredSlot triggeredCondition; 
	}

	SelectedEvent SelectEventFor(PlanSlot slot, PlanSchemaSlot schemaSlot, EventSide when)
	{
		this.FilterAvailableEvents();

		Random.state = this.executorSaveData.randomState;

		// NOTE(elliot): events should be sorted by priority at this point, so higher priority events will get a chance to go first
		SelectedEvent result = new SelectedEvent();
		for (int eventDataIndex = 0; eventDataIndex < this.filteredEvents.Count; ++eventDataIndex) {
			EventData def = this.filteredEvents[eventDataIndex];
			EventConditions conditions = def.conditions;
			DesiredSlot[] slotConditions = conditions.slotConditions;
			if (slotConditions != null) {
				for (int conditionIndex = 0; conditionIndex < slotConditions.Length; ++conditionIndex) {
					bool conditionPassed = false;
					DesiredSlot slotCondition = slotConditions[conditionIndex];
					if (slotCondition.when == when) {
						if (slotCondition.type == slot.slotType) {
							conditionPassed = true;
						} else if (slotCondition.time >= 0 && slotCondition.time > schemaSlot.unitIndex && slotCondition.time < schemaSlot.unitIndex + schemaSlot.unitLength) {
							conditionPassed = true;
						}
					}
					if (conditionPassed) {
						float randomValue = Random.value;
						if (randomValue <= slotCondition.chance) {
							// Condition & random chance passed, select this event
							result.def = def;
							result.triggeredCondition = slotCondition;
							break;
						}
					}
				}
				if (result.def != null) {
					break;
				}
			}
		}

		this.executorSaveData.randomState = Random.state;

		return result;
	}
}

}
