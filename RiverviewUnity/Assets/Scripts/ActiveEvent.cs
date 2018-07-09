using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

public enum EventProgressResult
{
	Continue,
	Done
}

[System.Serializable]
public class ActiveEvent
{
	public EventData def;
	[System.NonSerialized]
	public Nav.VisibleEnvScene envScene;
	[System.NonSerialized]
	public Cast cast;

	bool firstTime = true;

	public EventProgressResult Progress()
	{
		Debug.Assert(cast != null);
		EventProgressResult result = EventProgressResult.Continue;
		if (envScene != null) {
			if (firstTime) { firstTime = false; Debug.Log("***EVENT*** Press space to continue..."); }
			if (Input.GetKey(KeyCode.Space)) { result = EventProgressResult.Done; }
		}
		return result;
	}
}

}
