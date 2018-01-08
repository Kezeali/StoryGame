using UnityEngine;

namespace NotABear
{

public enum Weekday
{
	Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
}

public class WeeklyPlanSectionUI : PlanSectionUI
{
	public Weekday weekday;

	public override int PlanSectionUnitIndex() { return (int)weekday; }
}

}
