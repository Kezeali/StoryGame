using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

// A cast of characters--for example in a scene, or the main game cast. Serialized into save games.
[System.Serializable]
public class Cast
{
	public Character pc;
	public List<Character> leadNpcs;
	public int nextId;

	public string GenId(RoleData role)
	{
		return Strf.Format("{0}-{1}", role.name, nextId);
	}

	public void PostLoadCleanup(DataIndex gameData)
	{
		if (this.leadNpcs == null)
		{
			this.leadNpcs = new List<Character>();
		}
		if (this.pc == null || this.pc.role != gameData.playerRole)
		{
			this.pc = Character.Generate(gameData.playerRole, this);
		}

		this.pc.PostLoadCleanup();
		for (int i = 0; i < this.leadNpcs.Count; ++i)
		{
			this.leadNpcs[i].PostLoadCleanup();
		}
	}

	public void FixReferences()
	{
		this.pc.FixReferences(this);
		for (int i = 0; i < this.leadNpcs.Count; ++i)
		{
			this.leadNpcs[i].FixReferences(this);
		}
	}

	public void UpdateStats(int time)
	{
		this.pc.UpdateStats(time);
		for (int i = 0; i < this.leadNpcs.Count; ++i)
		{
			this.leadNpcs[i].UpdateStats(time);
		}
	}

	public Character FindCastMember(RoleData role)
	{
		if (role == this.pc.role) {
			return this.pc;
		} else {
			for (int i = 0; i < this.leadNpcs.Count; ++i) {
				if (role == this.leadNpcs[i].role) {
					return this.leadNpcs[i];
				}
			}
		}
		return null;
	}

	public Character FindCastMember(CharacterRef characterRef)
	{
		Character character = null;
		if (characterRef.characterName == this.pc.name) {
			return this.pc;
		} else {
			for (int i = 0; i < this.leadNpcs.Count; ++i) {
				if (characterRef.characterName == this.leadNpcs[i].name) {
					return this.leadNpcs[i];
				}
			}
		}
		return character;
	}

	public static void ApplyNewStatuses(Cast destCast, Cast liveCast)
	{
		destCast.pc.ApplyStatus(liveCast.pc);
		for (int simNpcIndex = 0; simNpcIndex < liveCast.leadNpcs.Count; ++simNpcIndex) {
			Character simNpc = liveCast.leadNpcs[simNpcIndex];
			Character actualNpc = null;
			for (int npcIndex = 0; npcIndex < destCast.leadNpcs.Count; ++npcIndex) {
				if (destCast.leadNpcs[npcIndex].name == simNpc.name) {
					actualNpc = destCast.leadNpcs[npcIndex];
				}
			}
			if (actualNpc != null) {
				actualNpc.ApplyStatus(simNpc);
			} else {
				Debug.LogWarningFormat("NPC has gone missing: expected NPC with name '{0}'", simNpc.name);
			}
		}
	}
}

}