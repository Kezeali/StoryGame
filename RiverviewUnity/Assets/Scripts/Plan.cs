using System.Collections.Generic;

namespace Cloverview
{
	[System.Serializable]
	public sealed class Plan
	{
		public string name;

		public PlanSchema schema;

		public PlanSection[] sections = new PlanSection[0];
	}

	// A section of the plan such as a day or week
	[System.Serializable]
	public sealed class PlanSection
	{
		public PlanSlot[] slots = new PlanSlot[0];
	}

	[System.Serializable]
	public sealed class PlanSlot
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
