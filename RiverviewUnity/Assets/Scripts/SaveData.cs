using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{
	[System.Serializable]
	public class SaveData
	{
		public string name;

		public Plan weeklyPlan;
		public Character pc;
		public List<Character> leadNpcs;
		public NavSaveData nav;
		public Dictionary<string, PlanExecutorSaveData> planExecutors;
		public Random.State randomState;
	}

	[System.Serializable]
	public class NavSaveData
	{
		public List<Nav.VisibleMenu> popupStack;
		public List<Nav.VisibleEnvScene> visibleEnvScenes;

		public int nextPreloadId;
	}

	[System.Serializable]
	public class PlanExecutorSaveData
	{
		public int timeUnitsElapsed;
		public Random.State randomState;
		public Cast liveCast;
	}
}
