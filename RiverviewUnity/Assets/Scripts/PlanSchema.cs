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
		public int totalTimeUnits;
		public PlanSchemaSlot[] slots = new PlanSchemaSlot[0];
	}

	[System.Serializable]
	public class PlanSchemaSlot
	{
		public int unitIndex;
		public int unitLength;
		public SlotType slotType;
	}

	public static class SchemaStuff
	{
		public static Plan CreateBlankPlan(PlanSchema schema, string planName)
		{
			int sectionsCount = schema.sections.Length;

			var plan = new Plan();
			plan.name = planName;

			plan.sections = new PlanSection[sectionsCount];
			for (int newSectionIndex = 0; newSectionIndex < sectionsCount; ++newSectionIndex)
			{
				PlanSchemaSection schemaSection = schema.sections[newSectionIndex];

				var planSection = new PlanSection();
				plan.sections[newSectionIndex] = planSection;

				int slotsCount = schemaSection.slots.Length;
				planSection.slots = new PlanSlot[slotsCount];
				for (int slotIndex = 0; slotIndex < slotsCount; ++slotIndex)
				{
					PlanSchemaSlot schemaSlot = schemaSection.slots[slotIndex];

					var planSlot = new PlanSlot();
					planSlot.unitIndex = schemaSlot.unitIndex;
					planSlot.slotType = schemaSlot.slotType;
					planSection.slots[slotIndex] = planSlot;
				}
			}
			return plan;
		}

		public static void UpgradePlan(PlanSchema schema, Plan schemaMatchingPlan, Plan loadedPlan)
		{
			// NOTE(elliot): this doesn't work if they're the same object! (because the matching plan is cleared below)
			UnityEngine.Debug.Assert(schemaMatchingPlan != loadedPlan);
			if (schemaMatchingPlan == loadedPlan) {
				return;
			}
			schemaMatchingPlan.ClearSelections();

			int sectionsCount = schema.sections.Length;
			for (int sectionIndex = 0; sectionIndex < sectionsCount; ++sectionIndex)
			{
				PlanSchemaSection schemaSection = schema.sections[sectionIndex];
				PlanSection planSection = schemaMatchingPlan.sections[sectionIndex];

				if (loadedPlan != null && loadedPlan.sections.Length > sectionIndex)
				{
					PlanSection loadedSection = loadedPlan.sections[sectionIndex];

					// Try to transfer the content of each slot
					for (int loadedSlotIndex = 0; loadedSlotIndex < loadedSection.slots.Length; ++loadedSlotIndex)
					{
						PlanSlot loadedSlot = loadedSection.slots[loadedSlotIndex];

						if (loadedSlot.selectedOption == null)
						{
							continue;
						}

						int loadedSlotUnitBegin = loadedSlot.unitIndex;
						int selectedOptionLength = 0;
						if (loadedSlot.selectedOption != null && loadedSlot.selectedOption.plannerItem != null)
						{
							selectedOptionLength = loadedSlot.selectedOption.plannerItem.timeUnits;
						}
						// this is just a guess, but that's fine
						// TODO(elliot): consider saving the schema for each plan so this doesn't have to be guessed?
						int loadedSlotUnitEnd = loadedSlotUnitBegin + selectedOptionLength;

						SlotType requiredType = loadedSlot.slotType;

						// Look for the first empty & valid slot that overlaps the given entry
						for (int actualSlotIndex = 0; actualSlotIndex < planSection.slots.Length; ++ actualSlotIndex)
						{
							PlanSchemaSlot schemaSlot = schemaSection.slots[actualSlotIndex];
							PlanSlot actualSlot = planSection.slots[actualSlotIndex];

							if (actualSlot.slotType == requiredType && actualSlot.selectedOption == null)
							{
								int actualUnitLength = schemaSlot.unitLength;
								int actualBegin = schemaSlot.unitIndex;
								int actualEnd = actualBegin + actualUnitLength;

								// NOTE(elliot): this should check whether both, 1) the loaded slot overlaps the slot currently being checked (called "actualSlot", as it is a slot that is actually in the current schema), and 2) the selected option in the loaded slot will fit in the actual slot
								if (((loadedSlotUnitBegin > actualBegin && loadedSlotUnitBegin <= actualEnd) || (loadedSlotUnitEnd > actualBegin && loadedSlotUnitEnd <= actualEnd))
									&& selectedOptionLength <= actualUnitLength)
								{
									actualSlot.selectedOption = loadedSlot.selectedOption;
									break;
								}
							}
						}
					}
				}
			}
		}

		public static bool PlanFitsSchema(PlanSchema schema, Plan plan)
		{
			// TODO(elliot): implement
			return true;
		}
	}
}
