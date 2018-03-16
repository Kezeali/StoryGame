using UnityEngine;

namespace Cloverview
{

public enum Weekday
{
	Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
}

public class WeeklyPlanSectionUI : PlanSectionUI
{
	public Weekday weekday;

	public int totalHours;

	public void OnValidate()
	{
		this.totalHours = Mathf.Max(this.totalHours, base.TotalTimeUnits());
	}

	public override int SectionUnitIndex() { return (int)this.weekday; }

	public override int TotalTimeUnits()
	{
		return this.totalHours;
	}
}

}
