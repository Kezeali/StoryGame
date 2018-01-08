using System.Collections.Generic;

namespace NotABear
{
	[System.Serializable]
	public class Plan
	{
		public string name;

		public PlanSection[] sections;
	}

	// A closed section of the plan such as a day or week
	[System.Serializable]
	public class PlanSection
	{
		public List<FilledPlanSlot> filledSlots = new List<FilledPlanSlot>();
	}

	[System.Serializable]
	public class FilledPlanSlot
	{
		public PlanOption selectedOption;
	}
}
