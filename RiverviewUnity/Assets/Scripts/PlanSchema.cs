using System.Collections.Generic;

namespace Cloverview
{
	[System.Serializable]
	public class PlanSchema
	{
		public string name;

		public PlanSchemaSection[] sections = new PlanSchemaSection[0];
	}

	// A section of the plan such as a day or week
	[System.Serializable]
	public class PlanSchemaSection
	{
		int totalTimeUnits;
		public PlanSchemaSlot[] slots = new PlanSchemaSlot[0];
	}

	[System.Serializable]
	public class PlanSchemaSlot
	{
		public int unitIndex;
		public int unitLength;
		public SlotType slotType;
	}
}
