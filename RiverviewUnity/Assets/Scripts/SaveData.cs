using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

[System.Serializable]
public sealed class SaveData
{
	public string name;

	public string displayName;

	// This field is set when the player makes a new named save during a game, and allows a tree-view to be generated for save games showing their heratage
	public string parentSaveName;

	public int time;
	public List<Plan> plans;
	public Cast cast;
	public NavSaveData nav;
	public PlanExecutorSaveData planExecutor;
	public Random.State randomState;
}

[System.Serializable]
public sealed class NavSaveData
{
	public Nav.VisibleMenu currentRootMenu;
	public List<MenuData> breadcrumbs;

	public int nextPreloadId;
}

[System.Serializable]
public sealed class PlanExecutorSaveData
{
	public int timeUnitsElapsed;
	public int commutesFinishedUpToTime;
	public Random.State randomState;
	public MenuData backMenu;
	public Plan livePlan;
	public Cast liveCast;
	public ActiveEvent activeEvent;
}

[System.Serializable]
public sealed class AppSaveData
{
	public string selectedProfileName;
}

[System.Serializable]
public sealed class ProfileSaveData
{
	public string name;
	public string displayName;
	public string selectedSaveName;
}

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
		if (this.pc == null)
		{
			this.pc = Character.Generate(gameData.playerRole, this);
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

	public void UpdateStatBonuses(int time)
	{
		this.pc.UpdateStatBonuses(time);
		for (int i = 0; i < this.leadNpcs.Count; ++i)
		{
			this.leadNpcs[i].UpdateStatBonuses(time);
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
