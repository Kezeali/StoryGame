using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{
// A section of a calendar which appears in a planning UI
[System.Serializable]
public class PlanSchema : ScriptableObject, IDataItem
{
	// TODO: when entering a PlanUI in play mode in the editor, pop up a dialgue asking if the developer wants to update the loaded PlanSchema asset to match it if it's out of date

	public PlanSchemaSection[] sections = new PlanSchemaSection[0];

	int totalTimeUnits = -1;
	public int GetTotalTimeUnits()
	{
		if (this.totalTimeUnits == -1) {
			for (int sectionIndex = 0; sectionIndex < this.sections.Length; ++sectionIndex) {
				PlanSchemaSection schemaSection = this.sections[sectionIndex];
				this.totalTimeUnits += schemaSection.totalTimeUnits;
			}
		}
		return this.totalTimeUnits;
	}
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
	public int start;
	public int duration;
	public SlotType slotType;
}

public static class SchemaStuff
{
	public static Plan CreateBlankPlan(PlanSchema schema, string planName)
	{
		int sectionsCount = schema.sections.Length;

		var plan = new Plan();
		plan.name = planName;
		plan.schema = schema;

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
				planSlot.start = schemaSlot.start;
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
		UnityEngine.Debug.Assert(schemaMatchingPlan.schema == schema);
		if (schemaMatchingPlan.schema != schema) {
			UnityEngine.Debug.LogErrorFormat("Can't upgrade plan as the destination plan '{0}' (schema '{1}') doesn't match the destination schema '{2}'!", schemaMatchingPlan, schemaMatchingPlan.schema, schema);
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

					int loadedSlotUnitBegin = loadedSlot.start;
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
							int actualUnitLength = schemaSlot.duration;
							int actualBegin = schemaSlot.start;
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
