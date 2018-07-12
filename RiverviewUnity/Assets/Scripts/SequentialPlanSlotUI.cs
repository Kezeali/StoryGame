using UnityEngine;

namespace Cloverview
{

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
