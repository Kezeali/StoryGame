using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

// Save data. Serialized to / from YAML in save games.
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
	public List<PlanOptionSelectorState> planOptionSelectorStates;
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
public class PlanOptionSelectorState
{
	public string name;
	public List<PlanSectionUIState> planSectionUIStates;
}

[System.Serializable]
public class PlanSectionUIState
{
	public string name;
	public bool open;
}

}
