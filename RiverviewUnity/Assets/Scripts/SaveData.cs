using System.Collections.Generic;

namespace NotABear
{
	[System.Serializable]
	public class SaveData
	{
		public string name;

		public Plan weeklyPlan;
		public Character pc;
		public NavSaveData nav;
		public PlanExecutorSaveData weeklyPlanExecutor;
		public int randomSeed;
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
		public int eventSeed;
	}
}
