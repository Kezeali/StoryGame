using UnityEngine;
using NotABear;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NotABear
{

public class PlanExecutor : MonoBehaviour, IDataUser<SaveData>, IDataUser<Nav>, INavigator
{
	[SerializeField]
	MenuData executeMenu;

	[ReadOnly]
	public string parentScene;

	SaveData saveData;
	Nav nav;
	Plan plan;
	string activityScenePreloadId;
	List<PlanActivityData> preloadedActivities = new List<PlanActivityData>();
	List<PlanActivityData> nextPreloadedActivities = new List<PlanActivityData>();
	// List<EventData> preloadedEvents = new List<EventData>();

	// NOTE: these are not applied to the character until the plan finishes executing. This allows the game to be saved and reloaded while the plan is being executed, and if the plan data / schema changes the character wont get or miss extra stat changes.
	Character.Status statChangesInProgress;

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.Register<Nav>(this);
	}

	public void OnDisable()
	{
		this.UnloadAllPreloadedActivities();
	}

	public void Initialise(SaveData saveData)
	{
		Debug.Assert(saveData != null);

		this.saveData = saveData;

		// TODO: re-execute up to the saved timeUnit
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);

		this.nav = nav;

		this.activityScenePreloadId = this.nav.GeneratePreloadIdForEnvScenes();

		this.nav.Preload(this.executeMenu, this.parentScene);

		this.PreloadPlanActivities();
	}

	public void Initialise(Plan plan)
	{
		this.plan = plan;
		this.PreloadPlanActivities();
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
				for (int slotIndex = 0; slotIndex < this.plan.sections.Length; ++slotIndex)
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
			this.preloadedActivities = temp;

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
		this.nav.GoTo(this.executeMenu, this.parentScene);
		this.PreloadPlanActivities();

		this.StartCoroutine(this.ExecuteCoroutine(instantlyExecuteTimeUnits));
	}

	IEnumerator ExecuteCoroutine(int instantlyExecuteTimeUnits)
	{
		// foreach activity, execute
		yield return 0;
	}

	IEnumerator ExecuteActivity(PlanOption option, bool instant)
	{
		Debug.Assert(option.plannerItem != null);
		if (option.plannerItem != null)
		{
			PlanActivityData activity = option.plannerItem.activity;
			if (!instant)
			{
				this.nav.GoToActivity(activity, this.activityScenePreloadId);
			}

			while (false)
			{
				// this.statChangesInProgress = activeActivity.Progress(this.statChangesInProgress);
				if (!instant)
				{
					yield return 0;
				}
			}
		}
	}
}

}
