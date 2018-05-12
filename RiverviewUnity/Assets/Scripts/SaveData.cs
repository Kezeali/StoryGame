using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{
	[System.Serializable]
	public class SaveData
	{
		public string name;

		public int time;
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
		public Nav.VisibleMenu currentRootMenu;

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
