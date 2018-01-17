using System.Collections.Generic;

namespace NotABear
{
	[System.Serializable]
	public class Plan
	{
		public string name;

		public PlanSection[] sections;
	}

	// A section of the plan such as a day or week
	[System.Serializable]
	public class PlanSection
	{
		public PlanSlot[] slots;
	}

	[System.Serializable]
	public class PlanSlot
	{
		public int unitIndex;
		public SlotType slotType;
		public PlanOption selectedOption;
	}
}
