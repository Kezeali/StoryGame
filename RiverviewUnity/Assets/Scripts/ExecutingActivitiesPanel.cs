using UnityEngine;
using UnityEngine.UI;
using Cloverview;
using System.Collections.Generic;

namespace Cloverview
{

public class ExecutingActivitiesPanel : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
{
	// TODO(elliot): make this a list of executor ids to look for?
	[SerializeField]
	string executorId;

	[SerializeField]
	Text activityNameText;

	PlanExecutor executor;

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.instance.GetExecutor(executorId, this);
	}

	public void OnDisable()
	{
		App.Register<SaveData>(this);
		App.instance.CancelRequestForExecutor(executorId, this);
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
		string currentActivityName = executor.currentActivityName;
		this.activityNameText.text = currentActivityName;
	}
}

}
