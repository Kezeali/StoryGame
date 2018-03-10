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

	public override int SectionUnitIndex() { return (int)weekday; }
}

}
