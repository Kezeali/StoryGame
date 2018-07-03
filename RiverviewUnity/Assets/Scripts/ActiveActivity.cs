using System.Collections.Generic;

namespace Cloverview
{

	public class ActiveActivity
	{
		public PlanActivityData def;
		public SubjectData subjectDef;
		public Nav.VisibleEnvScene envScene;
		public Cast cast;
		public int beginTimeUnit;
		public int timeUnitsSpent;

		public void Progress()
		{
		}

		public void Finish()
		{
			StatBonusSource source = new StatBonusSource()
			{
				name = def.name,
				type = StatBonusSource.SourceType.Activity
			};
			this.cast.pc.AddStatBonuses(this.def.statBonuses, source, this.beginTimeUnit, this.timeUnitsSpent);
			this.cast.pc.AddStatBonuses(this.subjectDef.statBonuses, source, this.beginTimeUnit, this.timeUnitsSpent);
		}
	}

}
