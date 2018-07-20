using UnityEngine;
using Cloverview;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class PlanExecutor : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<Nav>, IServiceUser<PlannerDataIndex>, INavigator
{
	[SerializeField]
	float defaultSecondsPerUnitTime;

	[SerializeField]
	MenuData executeMenu;

	[SerializeField]
	[UnityEngine.Serialization.FormerlySerializedAs("backMenu")]
	MenuData defaultBackMenu;

	[SerializeField]
	SceneData homeScene;

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
	PlannerDataIndex plannerData;
	string activityScenePreloadId;
	List<PlanActivityData> preloadedActivities = new List<PlanActivityData>();
	List<PlanActivityData> nextPreloadedActivities = new List<PlanActivityData>();
	List<RatedEvent> sortedEvents;

	bool executing;

	public const float MIN_SECONDS_PER_UNIT_TIME =  (1.0f / 120.0f);

	public void Reset()
	{
		this.defaultSecondsPerUnitTime = 2;
	}

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.Register<Nav>(this);
		App.Register<PlannerDataIndex>(this);
	}

	public void OnDisable()
	{
		App.Deregister<SaveData>(this);
		App.Deregister<Nav>(this);
		App.Deregister<PlannerDataIndex>(this);

		if (this.nav != null) {
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
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);

		this.nav = nav;
	}

	public void Initialise(PlannerDataIndex plannerData)
	{
		Debug.Assert(nav);

		this.plannerData = plannerData;
	}

	public void CompleteInitialisation()
	{
		this.activityScenePreloadId = this.nav.GeneratePreloadIdForEnvScenes();

		if (this.executing) {
			// Just stop executing. Assume that all the other systems will handle their own problems fixing the game state (like Nav going to the correct scenes.)
			this.StopAllCoroutines();
			this.executing = false;
		}

		if (!string.IsNullOrEmpty(this.parentScene)) {
			this.nav.Preload(this.executeMenu, this.parentScene);
		}

		this.LoadSave();
		if (!this.ResumeExecutionIfReady()) { this.PreloadIfReady(); }
	}

	void LoadSave()
	{
		Debug.Assert(!this.executing);

		if (this.saveData != null) {
			Debug.LogFormat("Loading save data for executor key '{0}'", this.key);
			if (this.saveData.planExecutor == null) {
				this.saveData.planExecutor = new PlanExecutorSaveData();
			}
			this.executorSaveData = this.saveData.planExecutor;
		}
	}

	bool IsReadyToResume()
	{
		return this.executorSaveData != null && this.executorSaveData.timeUnitsElapsed > 0 && this.executorSaveData.livePlan != null;
	}

	public string GetExpectedPlanName()
	{
		return this.expectedPlanName;
	}

	bool PreloadIfReady()
	{
		if (this.RequiredServicesAreInitialised()) {
			this.PreloadPlanActivities();
			return true;
		}
		return false;
	}
 
	bool ResumeExecutionIfReady()
	{
		if (this.RequiredServicesAreInitialised() &&
			this.executorSaveData.timeUnitsElapsed > 0 &&
			this.isActiveAndEnabled)
		{
			this.ExecuteInternal(0);
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

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}

	public void OnOptionSelected(PlanOption selectedOption)
	{
		if (this.RequiredServicesAreInitialised()) {
			this.PreloadPlanActivities();
		}
	}

	public void OnOptionDeselected(PlanOption selectedOption)
	{
		if (this.RequiredServicesAreInitialised()) {
			this.PreloadPlanActivities();
		}
	}

	void PreloadPlanActivities()
	{
		UnityEngine.Profiling.Profiler.BeginSample("PlanExecutor.PreloadPlanActivities");
		this.nextPreloadedActivities.Clear();

		int fromTime = this.saveData.time;
		PlanDateTime currentDateTime = PlanDateTime.FromTimeUnits(this.plannerData, fromTime);

		PlanSchema currentSchema = currentDateTime.GetSchema();
		if (currentSchema == null) {
			UnityEngine.Profiling.Profiler.EndSample();;
			Debug.LogError("Can't preload activities for current planning period as the schema is missing.");
			return;
		}
		
		string currentPlanName = currentSchema.name;

		for (int planIndex = 0; planIndex < this.saveData.plans.Count; ++planIndex) {
			Plan plan = this.saveData.plans[planIndex];
			if (plan.name != currentPlanName) {
				continue;
			}
			for (int sectionIndex = 0; sectionIndex < plan.sections.Length; ++sectionIndex) {
				PlanSection section = plan.sections[sectionIndex];
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
		}
		var temp = this.preloadedActivities;
		this.preloadedActivities = this.nextPreloadedActivities;
		this.nextPreloadedActivities = temp;

		this.nextPreloadedActivities.Clear();
		UnityEngine.Profiling.Profiler.EndSample();
	}

	void UnloadAllPreloadedActivities()
	{
		for (int i = 0; i < this.preloadedActivities.Count; ++i) {
			PlanActivityData activity = this.preloadedActivities[i];
			this.nav.RemovePreloadRequest(activity.scene, this.activityScenePreloadId);
		}
	}

	public bool IsReadyForPlayerToExecute()
	{
		return this.RequiredServicesAreInitialised() && !this.executing;
	}

	public void BeginExecution()
	{
		this.BeginExecution(0, 0);
	}

	public void BeginExecution(int skipTimeUnits, int instantTimeUnits)
	{
		if (this.RequiredServicesAreInitialised() && !this.executing) {
			this.executorSaveData.timeUnitsElapsed = skipTimeUnits;
			if (skipTimeUnits != 0) {
				this.InitExecution();
			}
			this.ExecuteInternal(instantTimeUnits);
		}
	}

	void InitExecution()
	{
		UnityEngine.Profiling.Profiler.BeginSample("PlanExecutor.InitExecution");
		this.executorSaveData.livePlan = null;

		// Find the plan at the current time
		PlanDateTime currentDateTimeInStream = PlanDateTime.FromTimeUnits(this.plannerData, this.saveData.time);
		PlanSchema currentSchema = currentDateTimeInStream.GetSchema();
		for (int i = 0; i < this.saveData.plans.Count; ++i) {
			Plan plan = this.saveData.plans[i];
			if (plan.schema == currentSchema) {
				this.executorSaveData.livePlan = plan;
				break;
			}
		}

		if (this.executorSaveData.livePlan == null) {
			Debug.LogError("Can't execute as there is no plan available for the current planning period.");
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	void ExecuteInternal(int instantTimeUnits)
	{
		if (!this.executing) {
			this.gameObject.SetActive(true);
			this.StartCoroutine(this.ExecuteCoroutine(this.defaultSecondsPerUnitTime, instantTimeUnits));
		} else {
			Debug.LogWarning("Tried to execute plan again while already executing!");
		}
	}

	// Begins or resumes execution depending on the contents of executorSaveData
	IEnumerator ExecuteCoroutine(float secondsPerUnitTime, int instantTimeUnits)
	{
		this.executing = true;
		
		if (this.executorSaveData.timeUnitsElapsed == 0 || this.executorSaveData.livePlan == null) {
			// Not resuming, set up to begin executing the current plan
			this.InitExecution();

			if (this.executorSaveData.livePlan == null) {
				this.executing = false;
				yield break;
			}
		}

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

		int skipTimeUnits = this.executorSaveData.timeUnitsElapsed;

		Cast liveCast = null;
		if (skipTimeUnits > 0) {
			liveCast = this.executorSaveData.liveCast;
		}
		if (liveCast == null) {
			liveCast = new Cast();
		}
		if (liveCast.pc == null) {
			liveCast.pc = this.saveData.cast.pc.CreateSimulationClone();
		}
		if (liveCast.leadNpcs == null) {
			liveCast.leadNpcs = new List<Character>(this.saveData.cast.leadNpcs.Count);
			for (int npcIndex = 0; npcIndex < this.saveData.cast.leadNpcs.Count; ++npcIndex) {
				Character realNpc = this.saveData.cast.leadNpcs[npcIndex];
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

		Plan livePlan = this.executorSaveData.livePlan;
		PlanSchema liveSchema = livePlan.schema;

		int localTimeUnitsElapsed = 0;
		int sectionLevelTimeUnitsElapsed = 0;
		for (int sectionIndex = 0; sectionIndex < liveSchema.sections.Length; ++sectionIndex) {
			PlanSchemaSection schemaSection = liveSchema.sections[sectionIndex];
			PlanSection planSection = livePlan.sections[sectionIndex];

			localTimeUnitsElapsed = sectionLevelTimeUnitsElapsed;

			for (int slotIndex = 0; slotIndex < schemaSection.slots.Length; ++slotIndex) {
				PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];
				PlanSlot slot = planSection.slots[slotIndex];

				localTimeUnitsElapsed = sectionLevelTimeUnitsElapsed + schemaSlot.start;

				// NOTE(elliot): Slot must be completely covered by the skip/instantly time units range (because being completely covered indicates that, if the schema hasn't changed, that slot was finished when this save file was created)
				bool skipSlot = localTimeUnitsElapsed + schemaSlot.duration <= skipTimeUnits;
				bool instantSlot = localTimeUnitsElapsed + schemaSlot.duration <= instantTimeUnits;

				if (!skipSlot) {
					PlanActivityData activityData = GetActivity(slot);

					// NOTE(elliot): even if the slot itself isn't skipped, skip commute-to events if the skip time is beyond the start of the slot
					bool skipCommuteTo = localTimeUnitsElapsed <= this.executorSaveData.commutesFinishedUpToTime;
					if (!skipCommuteTo) {
						SceneData destinationScene = null;
						if (activityData != null) {
							destinationScene = activityData.scene;
						}
						EventIncidenceTime incidenceTime = EventIncidenceTime.BeforeSlot;
						// NOTE(elliot): this block is the same for Before and After events, but it's duplicated to avoid creating another coroutine or a bespoke FSM (whahey!)
						{
							ActiveEvent activeEvent = null;
							// Resume the existing event, or start a new one
							if (this.executorSaveData.activeEvent != null) {
								activeEvent = this.executorSaveData.activeEvent;
								activeEvent.cast = liveCast;
								Debug.LogFormat("Resuming event {0}", activeEvent.def);
							} else {
								SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, liveCast, incidenceTime);
								if (selectedEvent.def != null) {
									activeEvent = new ActiveEvent()
									{
										def = selectedEvent.def,
										cast = liveCast
									};
									Debug.LogFormat("Spawning event {0}", activeEvent.def);
								}
							}
							if (activeEvent != null) {
								// Add the active event to the save data
								this.executorSaveData.activeEvent = activeEvent;
								
								this.nav.GoToCommute(activeEvent, destinationScene, this.activityScenePreloadId);
								while (this.nav.IsProcessingGoToEnvQueue() && activeEvent.envScene == null) {
									yield return 0;
								}

								EventProgressResult result = EventProgressResult.Continue;
								while (result == EventProgressResult.Continue) {
									result = activeEvent.Progress();
									yield return 0;
								}

								this.nav.FinishCommute(this.activityScenePreloadId);

								// The event has completed, commit status back to the primary save data
								Cast.ApplyNewStatuses(this.saveData.cast, liveCast);

								this.executorSaveData.commutesFinishedUpToTime = localTimeUnitsElapsed;
								this.executorSaveData.activeEvent = null;
							}
						}
					}

					//////////////////////////////////////////////
					// Execute the activity for this slot
					if (activityData != null) {
						int beginTimeUnit = localTimeUnitsElapsed;
						int slotLengthTimeUnits = schemaSlot.duration;
						int timeUnitsBeforeEvent = 0;

						// Determine if there will be event during this activity, and when to run it
						ActiveEvent activeEvent = null;
						if (this.executorSaveData.activeEvent != null) {
							// Resume the saved active event in progress
							activeEvent = this.executorSaveData.activeEvent;
							activeEvent.cast = liveCast;
							timeUnitsBeforeEvent = 0;
							Debug.LogFormat("Resuming event {0}", activeEvent.def);
						} else {
							SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, liveCast, EventIncidenceTime.DuringSlot);
							if (selectedEvent.def != null) {
								// NOTE(elliot): if a specific time was specified, rather than a slot type, this attempts to make the event occur at that time in the execution
								if (selectedEvent.triggeredCondition.time >= 0) {
									int desiredTimeUnitsBeforeEvent = selectedEvent.triggeredCondition.time - beginTimeUnit;
									int maxTimeUnitsBeforeEvent = slotLengthTimeUnits > 0 ? slotLengthTimeUnits-1 : 0;
									timeUnitsBeforeEvent = Mathf.Clamp(desiredTimeUnitsBeforeEvent, 0, maxTimeUnitsBeforeEvent);
								} else {
									// If no time was specified, the event occurs half-way through the slot
									timeUnitsBeforeEvent = slotLengthTimeUnits / 2;
								}
								activeEvent = new ActiveEvent()
								{
									def = selectedEvent.def,
									cast = liveCast
								};
								Debug.LogFormat("Spawning event {0}", activeEvent.def);
							}
						}
						
						/////////////////////
						// Spawn the activity
						Debug.LogFormat("Executing plannerItem: {0}", slot.selectedOption.plannerItem);
						var activeActivity = new ActiveActivity()
						{
							def = activityData,
							subjectDef = slot.selectedOption.plannerItem.subject,
							cast = liveCast,
							beginTimeUnit = beginTimeUnit
						};
						this.currentActivityName = slot.selectedOption.plannerItem.name;

						if (!instantSlot) {
							this.nav.GoToActivity(activeActivity, this.activityScenePreloadId);
							yield return 0;
						}

						liveCast.UpdateStats(beginTimeUnit);

						////////////////////////
						// Start the activity, assunming timeUnitsBeforeEvent > 0
						if (timeUnitsBeforeEvent > 0) {
							while (activeActivity.timeUnitsSpent < timeUnitsBeforeEvent) {
								activeActivity.Progress();

								liveCast.UpdateStats(beginTimeUnit + activeActivity.timeUnitsSpent);

								if (!instantSlot) {
									yield return new WaitForSeconds(secondsPerUnitTime);
								}
							}
							activeActivity.Pause();
						}

						////////////////////
						// Execute the selected event
						if (activeEvent != null) {
							Debug.LogFormat("Executing event {0}", activeEvent.def);

							// Add the active event to the save data
							this.executorSaveData.activeEvent = activeEvent;

							SceneData activityScene = activityData.scene;
							
							this.nav.GoToEvent(activeEvent, activityScene, this.activityScenePreloadId);
							while (this.nav.IsProcessingGoToEnvQueue() && activeEvent.envScene == null) {
								yield return 0;
							}

							EventProgressResult result = EventProgressResult.Continue;
							while (result == EventProgressResult.Continue) {
								result = activeEvent.Progress();
								yield return 0;
							}

							this.nav.FinishCommute(this.activityScenePreloadId);

							this.executorSaveData.activeEvent = null;
						}

						///////////////////
						// Continue the activity
						if (timeUnitsBeforeEvent > 0) {
							activeActivity.Resume();
						}
						while (activeActivity.timeUnitsSpent < slotLengthTimeUnits) {
							activeActivity.Progress();

							liveCast.UpdateStats(beginTimeUnit + activeActivity.timeUnitsSpent);

							if (!instantSlot) {
								yield return new WaitForSeconds(secondsPerUnitTime);
							}
						}

						activeActivity.Finish();

						// The activty has completed, commit status back to the primary save data
						Cast.ApplyNewStatuses(this.saveData.cast, liveCast);
					}
				}

				//////////////////////////////////////////
				// Determine if the skipTimeUnits point has been reached in the plan
				if (localTimeUnitsElapsed > skipTimeUnits) {
					this.executorSaveData.timeUnitsElapsed = localTimeUnitsElapsed;
					this.executorSaveData.randomState = Random.state;
					yield return 0;
				}

				///////////////////////////////
				// Execte any post-activity events (unless already finished in the loaded save)
				bool skipCommuteFrom = localTimeUnitsElapsed <= this.executorSaveData.commutesFinishedUpToTime;
				if (!skipCommuteFrom) {
					SceneData destinationScene = null;
					if (slotIndex < schemaSection.slots.Length-1) {
						PlanSlot nextSlot = planSection.slots[slotIndex+1];
						PlanActivityData nextActivity = GetActivity(nextSlot);
						if (nextActivity != null) {
							destinationScene = nextActivity.scene;
						}
					} else {
						destinationScene = this.homeScene;
					}
					EventIncidenceTime incidenceTime = EventIncidenceTime.AfterSlot;
					// NOTE(elliot): this block is the same for Before and After events, but it's duplicated to avoid creating another coroutine or a bespoke FSM (whahey!)
					{
						ActiveEvent activeEvent = null;
						// Resume the existing event, or start a new one
						if (this.executorSaveData.activeEvent != null) {
							activeEvent = this.executorSaveData.activeEvent;
							activeEvent.cast = liveCast;
							Debug.LogFormat("Resuming event {0}", activeEvent.def);
						} else {
							SelectedEvent selectedEvent = this.SelectEventFor(slot, schemaSlot, liveCast, incidenceTime);
							if (selectedEvent.def != null) {
								activeEvent = new ActiveEvent()
								{
									def = selectedEvent.def,
									cast = liveCast
								};
								Debug.LogFormat("Spawning event {0}", activeEvent.def);
							}
						}
						if (activeEvent != null) {
							// Add the active event to the save data
							this.executorSaveData.activeEvent = activeEvent;

							this.nav.GoToCommute(activeEvent, destinationScene, this.activityScenePreloadId);
							while (this.nav.IsProcessingGoToEnvQueue() && activeEvent.envScene == null) {
								yield return 0;
							}

							EventProgressResult result = EventProgressResult.Continue;
							while (result == EventProgressResult.Continue) {
								result = activeEvent.Progress();
								yield return 0;
							}

							this.nav.FinishCommute(this.activityScenePreloadId);

							// The event has completed, commit status back to the primary save data
							Cast.ApplyNewStatuses(this.saveData.cast, liveCast);

							this.executorSaveData.commutesFinishedUpToTime = localTimeUnitsElapsed;
							this.executorSaveData.activeEvent = null;
						}
					}
				}
			}
			sectionLevelTimeUnitsElapsed += schemaSection.totalTimeUnits;
		}

		// Update and commit the final statuses
		liveCast.UpdateStats(localTimeUnitsElapsed);
		Cast.ApplyNewStatuses(this.saveData.cast, liveCast);

		this.saveData.time += this.executorSaveData.timeUnitsElapsed;

		this.executorSaveData.livePlan = null;
		this.executorSaveData.liveCast = null;
		this.executorSaveData.timeUnitsElapsed = 0;
		// Copy the value before resetting it so it can be used to actually go back!
		MenuData nextMenu = this.executorSaveData.backMenu;
		this.executorSaveData.backMenu = null;

		Debug.Log("Plan done.");

		this.executing = false;

		this.UnloadAllPreloadedActivities();

		this.nav.GoTo(nextMenu, this.parentScene);
	}

	static PlanActivityData GetActivity(PlanSlot slot)
	{
		PlanActivityData result = null;
		if (slot.selectedOption != null && slot.selectedOption.plannerItem != null) {
			result = slot.selectedOption.plannerItem.activity;
		}
		return result;
	}

	struct SelectedEvent
	{
		public EventData def;
		public DesiredSlot triggeredCondition; 
	}

	SelectedEvent SelectEventFor(PlanSlot slot, PlanSchemaSlot schemaSlot, Cast liveCast, EventIncidenceTime when)
	{
		this.SortEvents(liveCast);

		Random.state = this.executorSaveData.randomState;

		// NOTE(elliot): events should be sorted by priority at this point, so higher priority events will get a chance to go first
		SelectedEvent result = new SelectedEvent();
		for (int sortedEventIndex = 0; sortedEventIndex < availableEventCount; ++sortedEventIndex) {
			RatedEvent ratedEvent = this.sortedEvents[sortedEventIndex];
			EventData def = ratedEvent.def;
			EventConditions conditions = def.conditions;
			DesiredSlot[] slotConditions = conditions.slotConditions;
			if (slotConditions != null) {
				for (int conditionIndex = 0; conditionIndex < slotConditions.Length; ++conditionIndex) {
					bool conditionPassed = false;
					DesiredSlot slotCondition = slotConditions[conditionIndex];
					if (slotCondition.when == when) {
						if (slotCondition.type == slot.slotType) {
							conditionPassed = true;
						} else if (slotCondition.time >= 0 && slotCondition.time > schemaSlot.start && slotCondition.time < schemaSlot.start + schemaSlot.duration) {
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

	// NOTE(elliot): events are filtered & sorted as follows:
	//  1) hard-no -- if any desired stat values return a hard-no result, the event is filtered out
	//  2) priority -- higher priority events which can occur will /always/ occur before lower priority events
	//  3) desired stats -- events are rated by how well their desired stats are fulfilled. higher rated events occur earlier

	class RatedEvent
	{
		public float rating;
		public EventData def;
	}

	EventDataSorter sorter = new EventDataSorter();
	int availableEventCount = 0;
	void SortEvents(Cast liveCast)
	{
		if (this.sortedEvents == null) {
			this.sortedEvents = new List<RatedEvent>(this.plannerData.events.Length);
		}
		if (this.sortedEvents.Count == 0) {
			for (int i = 0; i < this.plannerData.events.Length; ++i) {
				this.sortedEvents.Add(new RatedEvent() { def = this.plannerData.events[i]});
			}
		}
		this.sorter.cast = liveCast;
		for (int i = 0; i < this.sortedEvents.Count; i++) {
			this.sortedEvents[i].rating = PlanExecutor.Rate(this.sortedEvents[i].def, liveCast);
		}
		availableEventCount = this.sortedEvents.ExcludeAll(HasHardNo, 0);
		this.sortedEvents.Sort(0, availableEventCount, this.sorter);
	}

	static float Rate(EventData def, Cast liveCast)
	{
		float rating = DesiredStat.Rate(liveCast.pc.status, def.conditions.requiredPcStats);
		return rating;
	}

	static bool HasHardNo(RatedEvent ev, int unused)
	{
		return DesiredStat.IsHardNo(ev.rating);
	}

	class EventDataSorter : IComparer<RatedEvent>
	{
		public Cast cast;
		public int Compare(RatedEvent a, RatedEvent b)
		{
			if (a.def.conditions.priority < b.def.conditions.priority) {
				return -1;
			} else if (a.def.conditions.priority > b.def.conditions.priority) {
				return 1;
			} else {
				if (a.rating > b.rating) {
					return -1;
				} else if (a.rating < b.rating) {
					return 1;
				} else {
					return 0;
				}
			}
		}
	}
}

}
