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
	public string id;

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
	public float executingSecondsPerUnitTime;

	MenuData backMenu;
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
		this.others.Clear();
		if (this.nav != null)
		{
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
		for (int i = 0; i < existingExecutors.Length; ++i)
		{
			if (existingExecutors[i] != this.gameObject)
			{
				PlanExecutor otherExecutor = existingExecutors[i].GetComponent<PlanExecutor>();
				if (otherExecutor != null)
				{
					this.others.Add(otherExecutor);
				}
				else
				{
					Debug.LogErrorFormat("Non PlanExecutor taged with PlanExecutor tag: {0}", existingExecutors[i].name);
				}
			}
		}
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
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);

		this.nav = nav;

		this.activityScenePreloadId = this.nav.GeneratePreloadIdForEnvScenes();

		this.nav.Preload(this.executeMenu, this.parentScene);
	}

	public void CompleteInitialisation()
	{
		if (!this.ExecuteIfReady()) { this.PreloadIfReady(); }
	}

	public void SetPlan(Plan plan, PlanSchema planSchema)
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

	bool OthersExecuting()
	{
		bool result = false;
		for (int i = 0; i < this.others.Count; ++i)
		{
			if (this.others[i] != null && this.others[i].executing)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	bool ExecuteIfReady()
	{
		if (!this.OthersExecuting() &&
			this.DataReady() &&
			this.isActiveAndEnabled &&
			this.executorSaveData.timeUnitsElapsed > 0)
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
				PlanSlot previousSlot = null;
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

						if (previousSlot != null)
						{
							// TODO(elliot): preload commute scene from previous slot to this slot
							//this.nextPreloadedCommutes.Add(new ExpectedCommute(from, to));
						}
						else
						{
							// TODO(elliot): preload commute from home to first slot
						}
					}
					previousSlot = slot;
				}
				if (previousSlot != null)
				{
					// TODO(elliot): Preload commute from final slot to home
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

			this.nextFilteredEvents.Clear();

			for (int eventDataIndex = 0; eventDataIndex < this.availableEvents.Length; ++eventDataIndex)
			{
				EventData eventDef = this.availableEvents[eventDataIndex];
				Debug.Assert(!this.nextFilteredEvents.Contains(eventDef));
				EventConditions conditions = eventDef.conditions;
				if (conditions.requiredPcStats == null || !DesiredStat.DetermineHardNo(pc, conditions.requiredPcStats))
				{
					this.nextFilteredEvents.Add(eventDef);
				}
			}
			// TODO(elliot): cache the sorter to avoid extra garbage creation
			this.nextFilteredEvents.Sort(new EventDataSorter() { pc = pc });

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
		if (!this.executing)
		{
			if (!this.OthersExecuting())
			{
				this.StartCoroutine(this.ExecuteCoroutine(this.defaultSecondsPerUnitTime, skipTimeUnits, 0));
			}
			else
			{
				Debug.LogWarningFormat("Tried to execute while another executor ({0}) executing!", "TODO(elliot): get name");
			}
		}
		else
		{
			Debug.LogWarningFormat("Tried to execute again while {0} already executing!", this.id);
		}
	}

	IEnumerator ExecuteCoroutine(float secondsPerUnitTime, int skipTimeUnits, int instantTimeUnits)
	{
		this.executing = true;

		Object.DontDestroyOnLoad(this.gameObject);
		
		// Save the menu to return to
		if (this.nav.activeMenu != null && this.nav.activeMenu.def != null)
		{
			this.backMenu = this.nav.activeMenu.def;
		}
		else
		{
			this.backMenu = this.defaultBackMenu;
		}

		this.nav.GoTo(this.executeMenu, this.parentScene);
		this.PreloadPlanActivities();

		this.executingSecondsPerUnitTime = secondsPerUnitTime;

		Cast liveCast = null;
		if (skipTimeUnits > 0)
		{
			liveCast = this.executorSaveData.liveCast;
		}
		if (liveCast == null)
		{
			liveCast = new Cast();
		}
		if (liveCast.pc == null)
		{
			liveCast.pc = this.saveData.pc.CreateSimulationClone();
		}
		if (liveCast.leadNpcs == null)
		{
			liveCast.leadNpcs = new List<Character>(this.saveData.leadNpcs.Count);
			for (int npcIndex = 0; npcIndex < this.saveData.leadNpcs.Count; ++npcIndex)
			{
				Character realNpc = this.saveData.leadNpcs[npcIndex];
				liveCast.leadNpcs.Add(realNpc.CreateSimulationClone());
			}
		}
		this.executorSaveData.liveCast = liveCast;

		Random.InitState(1234818);
		this.executorSaveData.randomState = Random.state;

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
						SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.Before);
						if (selectedEvent.def != null)
						{
							Debug.LogFormat("Executing event {0}", selectedEvent.def);

							ActiveEvent activeEvent = new ActiveEvent()
							{
								def = selectedEvent.def,
								cast = liveCast
							};

							SceneData scene = null;
							PlanActivityData activityData = GetActivity(slot);
							if (activityData != null)
							{
								scene = activityData.scene;
							}

							this.nav.GoToCommute(activeEvent, scene, this.activityScenePreloadId);
							yield return 0;

							EventProgressResult result = EventProgressResult.Continue;
							while (result == EventProgressResult.Continue)
							{
								result = activeEvent.Progress();
								yield return 0;
							}

							this.nav.FinishCommute(this.activityScenePreloadId);
						}
					}

					IEnumerator op = this.ExecuteActivity(liveCast, slot, schemaSlot, this.saveData.time + localTimeUnitsElapsed, instantSlot ? 0 : secondsPerUnitTime);
					while (op.MoveNext())
					{
						if (!instantSlot)
						{
							yield return op.Current;
						}
					}
				}

				localTimeUnitsElapsed += schemaSlot.unitLength;

				if (localTimeUnitsElapsed > skipTimeUnits)
				{
					this.executorSaveData.timeUnitsElapsed = localTimeUnitsElapsed;
					yield return 0;
				}

				bool skipCommuteFrom = localTimeUnitsElapsed <= skipTimeUnits;
				if (!skipCommuteFrom)
				{
					SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.After);
					if (selectedEvent.def != null)
					{
						Debug.LogFormat("Executing event {0}", selectedEvent.def);

						ActiveEvent activeEvent = new ActiveEvent()
						{
							def = selectedEvent.def,
							cast = liveCast
						};

						SceneData scene = null;
						if (slotIndex < schemaSection.slots.Length-1)
						{
							PlanSlot nextSlot = planSection.slots[slotIndex+1];
							PlanActivityData activityData = GetActivity(nextSlot);
							if (activityData != null)
							{
								scene = activityData.scene;
							}
						}
						else
						{
							PlanActivityData activityData = GetActivity(slot);
							if (activityData != null)
							{
								scene = activityData.scene;
							}
						}

						this.nav.GoToCommute(activeEvent, scene, this.activityScenePreloadId);
						yield return 0;

						EventProgressResult result = EventProgressResult.Continue;
						while (result == EventProgressResult.Continue)
						{
							result = activeEvent.Progress();
							yield return 0;
						}

						this.nav.FinishCommute(this.activityScenePreloadId);
					}
				}
			}
		}

		this.saveData.pc.ApplyStatus(liveCast.pc);
		for (int simNpcIndex = 0; simNpcIndex < liveCast.leadNpcs.Count; ++simNpcIndex)
		{
			Character simNpc = liveCast.leadNpcs[simNpcIndex];
			Character actualNpc = null;
			for (int npcIndex = 0; npcIndex < this.saveData.leadNpcs.Count; ++npcIndex)
			{
				if (this.saveData.leadNpcs[npcIndex].name == simNpc.name)
				{
					actualNpc = this.saveData.leadNpcs[npcIndex];
				}
			}
			if (actualNpc != null)
			{
				actualNpc.ApplyStatus(simNpc);
			}
		}

		this.saveData.time += this.executorSaveData.timeUnitsElapsed;

		this.executorSaveData.liveCast = null;
		this.executorSaveData.timeUnitsElapsed = 0;

		Debug.Log("Plan done.");
		
		this.executing = false;

		this.UnloadAllPreloadedActivities();

		this.nav.GoTo(this.backMenu, this.parentScene);

		Object.Destroy(this.gameObject);
	}

	static PlanActivityData GetActivity(PlanSlot slot)
	{
		PlanActivityData result = null;
		if (slot.selectedOption != null && slot.selectedOption.plannerItem != null)
		{
			result = slot.selectedOption.plannerItem.activity;
		}
		return result;
	}

	IEnumerator ExecuteActivity(Cast liveCast, PlanSlot slot, PlanSchemaSlot schemaSlot, int beginTimeUnit, float secondsPerUnitTime)
	{
		bool instant = secondsPerUnitTime < MIN_SECONDS_PER_UNIT_TIME;
		if (slot.selectedOption != null)
		{
			int slotLengthTimeUnits = schemaSlot.unitLength;
			int timeUnitsBeforeEvent = slotLengthTimeUnits / 2;

			SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, EventSide.During);

			// NOTE(elliot): if a specific time was specified, rather than a slot type, this attempts to make the event occur at that time in the execution
			if (selectedEvent.def != null)
			{
				if (selectedEvent.triggeredCondition.time >= 0)
				{
					int desiredTimeUnitsBeforeEvent = selectedEvent.triggeredCondition.time - beginTimeUnit;
					int maxTimeUnitsBeforeEvent = slotLengthTimeUnits > 0 ? slotLengthTimeUnits-1 : 0;

					timeUnitsBeforeEvent = Mathf.Clamp(desiredTimeUnitsBeforeEvent, 0, maxTimeUnitsBeforeEvent);
				}
			}

			PlanOption option = slot.selectedOption;
			Debug.Assert(option.plannerItem != null);
			if (option.plannerItem != null)
			{
				PlanActivityData activity = option.plannerItem.activity;
				var activeActivity = new ActiveActivity()
				{
					def = activity,
					subjectDef = option.plannerItem.subject,
					cast = liveCast,
					beginTimeUnit = beginTimeUnit
				};

				Debug.LogFormat("Executing plannerItem: {0}", option.plannerItem);

				if (!instant)
				{
					this.nav.GoToActivity(activeActivity, this.activityScenePreloadId);
					yield return 0;
				}

				while (activeActivity.timeUnitsSpent < timeUnitsBeforeEvent)
				{
					activeActivity.Progress();
					activeActivity.timeUnitsSpent += 1;
					if (!instant)
					{
						yield return new WaitForSeconds(secondsPerUnitTime);
					}
				}

				if (selectedEvent.def != null)
				{
					Debug.LogFormat("Executing event {0}", selectedEvent.def);

					ActiveEvent activeEvent = new ActiveEvent()
					{
						def = selectedEvent.def,
						cast = liveCast
					};

					SceneData activityScene = null;
					if (!instant)
					{
						activityScene = activity.scene;
					}
					// NOTE(elliot): this is just to make sure the activity scene is passed to the event as soon as it loads (if it hasn't already). no actual commute is expected, as the activity scene should already be the current scene.
					this.nav.GoToCommute(activeEvent, activityScene, this.activityScenePreloadId);
					yield return 0;

					EventProgressResult result = EventProgressResult.Continue;
					while (result == EventProgressResult.Continue)
					{
						result = activeEvent.Progress();
						yield return 0;
					}

					this.nav.FinishCommute(this.activityScenePreloadId);
				}

				while (activeActivity.timeUnitsSpent < slotLengthTimeUnits)
				{
					activeActivity.Progress();
					activeActivity.timeUnitsSpent += 1;

					liveCast.pc.UpdateStatBonuses(beginTimeUnit + activeActivity.timeUnitsSpent);

					if (!instant)
					{
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
		for (int eventDataIndex = 0; eventDataIndex < this.filteredEvents.Count; ++eventDataIndex)
		{
			EventData def = this.filteredEvents[eventDataIndex];
			EventConditions conditions = def.conditions;
			DesiredSlot[] slotConditions = conditions.slotConditions;
			if (slotConditions != null)
			{
				for (int conditionIndex = 0; conditionIndex < slotConditions.Length; ++conditionIndex)
				{
					bool conditionPassed = false;
					DesiredSlot slotCondition = slotConditions[conditionIndex];
					if (slotCondition.when == when)
					{
						if (slotCondition.type == slot.slotType)
						{
							conditionPassed = true;
						}
						else if (slotCondition.time >= 0 && slotCondition.time > schemaSlot.unitIndex && slotCondition.time < schemaSlot.unitIndex + schemaSlot.unitLength)
						{
							conditionPassed = true;
						}
					}
					if (conditionPassed)
					{
						float randomValue = Random.value;
						if (randomValue <= slotCondition.chance)
						{
							// Condition & random chance passed, select this event
							result.def = def;
							result.triggeredCondition = slotCondition;
							break;
						}
					}
				}
				if (result.def != null)
				{
					break;
				}
			}
		}

		this.executorSaveData.randomState = Random.state;

		return result;
	}
}

}
