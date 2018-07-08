using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

[System.Serializable]
public class Cast
{
	public Character pc;
	public List<Character> leadNpcs;

	public static void UpdateStatBonuses(Cast liveCast, int time)
	{
		liveCast.pc.UpdateStatBonuses(time);
		for (int i = 0; i < liveCast.leadNpcs.Count; ++i)
		{
			liveCast.leadNpcs[i].UpdateStatBonuses(time);
		}
	}

	public Character FindCastMember(RoleData role)
	{
		if (role == this.pc.role) {
			return this.pc;
		} else {
			for (int i = 0; i < this.leadNpcs.Count; ++i) {
				if (this.leadNpcs[i].role == role) {
					return this.leadNpcs[i];
				}
			}
		}
		return null;
	}
}

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
