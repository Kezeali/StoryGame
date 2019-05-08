using UnityEngine;

namespace Cloverview
{

// Defines a PlanSlotUI that uses a sequential layout.
public class SequentialPlanSlotUI : PlanSlotUI
{
	[System.NonSerialized]
	public int start;
	[System.NonSerialized]
	public int duration;

	public override int GetStartTime() { return this.start; }
	public override int GetDuration() { return this.duration; }
}

}
