using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{
[System.Serializable]
public class SaveData
{
	public string name;

	public string displayName;

	// This field is set when the player makes a new named save during a game, and allows a tree-view to be generated for save games showing their heratage
	public string parentSaveName;

	public int time;
	public List<Plan> plans;
	public Character pc;
	public List<Character> leadNpcs;
	public NavSaveData nav;
	public Dictionary<string, PlanExecutorSaveData> planExecutors;
	public Random.State randomState;
}

[System.Serializable]
public class NavSaveData
{
	public Nav.VisibleMenu currentRootMenu;
	public List<MenuData> breadcrumbs;

	public int nextPreloadId;
}

[System.Serializable]
public class PlanExecutorSaveData
{
	public int timeUnitsElapsed;
	public Random.State randomState;
	public string planName;
	public PlanSchema liveSchema;
	public Cast liveCast;
}

[System.Serializable]
public class AppSaveData
{
	public string selectedProfileName;
}

[System.Serializable]
public class ProfileSaveData
{
	public string name;
	public string displayName;
	public string selectedSaveName;
}
}
