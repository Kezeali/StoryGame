using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class ExecutingActivitiesPanel : MonoBehaviour, IServiceUser<SaveData>, IServiceUser<PlanExecutor>
{
	// TODO(elliot): make a list of executor ids to look for?
	[SerializeField]
	string executorId;

	public void OnEnable()
	{
		App.Register<SaveData>(this);
		App.instance.GetExecutor(executorId, this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void Initialise(PlanExecutor planExecutor)
	{
	}
}

}
