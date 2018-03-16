using System.Collections.Generic;

namespace Cloverview
{
	[System.Serializable]
	public class Plan
	{
		public string name;

		public PlanSection[] sections = new PlanSection[0];
	}

	// A section of the plan such as a day or week
	[System.Serializable]
	public class PlanSection
	{
		public PlanSlot[] slots = new PlanSlot[0];
	}

	[System.Serializable]
	public class PlanSlot
	{
		public int unitIndex;
		public SlotType slotType;
		public PlanOption selectedOption;
	}

	public static class PlanMethods
	{
		public static void ClearSelections(this Plan plan)
		{
			for (int sectionIndex = 0; sectionIndex < plan.sections.Length; ++sectionIndex)
			{
				PlanSection section = plan.sections[sectionIndex];
				for (int slotIndex = 0; slotIndex < section.slots.Length; ++slotIndex)
				{
					PlanSlot slot = section.slots[slotIndex];
					slot.selectedOption = null;
				}
			}
		}
	}
}
