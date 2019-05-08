using UnityEngine;
using UnityEngine.UI;
using Cloverview;
using System.Collections.Generic;

namespace Cloverview
{

// UI script for displaying information about the activities in an executing calendar.
public class ExecutingActivitiesPanel : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
{
	// NOTE(elliot): if you want a single widget be able to display more than one type of executor, simply add multiple ExecutingActivitiesPanel components to it
	[SerializeField]
	PlanExecutor executorToDisplay;

	[SerializeField]
	Text activityNameText;

	PlanExecutor executor;

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.Register<PlanExecutor>(this);
	}

	public void OnDisable()
	{
		App.Deregister<SaveData>(this);
		App.Deregister<PlanExecutor>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void Initialise(PlanExecutor planExecutor)
	{
		this.executor = planExecutor;
	}

	public void CompleteInitialisation()
	{
	}

	public void Update()
	{
		if (this.executor != null)
		{
			string currentActivityName = this.executor.currentActivityName;
			this.activityNameText.text = currentActivityName;
		}
	}
}

}
