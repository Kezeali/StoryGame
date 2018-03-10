using UnityEngine;
using Cloverview;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{

public class EnvSceneController : MonoBehaviour, IDataUser<SaveData>
{
	public void OnEnable()
	{
		App.Register<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void AddEvent(ActiveEvent @event)
	{
	}

	public void SetActivity(ActiveActivity activity)
	{
	}

	public void ClearActivity()
	{
	}
}

}
