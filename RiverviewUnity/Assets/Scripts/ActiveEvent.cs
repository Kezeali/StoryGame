using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

[System.Serializable]
public class Cast
{
	public Character pc;
	public List<Character> leadNpcs;
}

public enum EventProgressResult
{
	Continue,
	Done
}

public class ActiveEvent
{
	public EventData def;
	public Nav.VisibleEnvScene envScene;
	public Cast cast;

	bool firstTime = true;

	public EventProgressResult Progress()
	{
		EventProgressResult result = EventProgressResult.Done;
		if (firstTime) { firstTime = false; Debug.Log("Press space to continue..."); }
		if (Input.GetKey(KeyCode.Space)) { result = EventProgressResult.Continue; }
		return result;
	}
}

}
