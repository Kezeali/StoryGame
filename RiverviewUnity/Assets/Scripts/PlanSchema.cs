using System.Collections.Generic;

namespace Cloverview
{
	// Could also be called "Calendar Definition", or "Plan Calendar"
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
		// Runtime mapping
		[System.NonSerialized]
		public PlanSection section;
	}

	[System.Serializable]
	public class PlanSchemaSlot
	{
		public int unitIndex;
		public int unitLength;
		// Runtime mapping
		[System.NonSerialized]
		public PlanSlot slot;
	}
}
