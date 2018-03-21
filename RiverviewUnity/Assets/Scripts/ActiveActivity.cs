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
			this.cast.pc.AddStatBonuses(this.def.statBonuses, this.beginTimeUnit, this.timeUnitsSpent);
			this.cast.pc.AddStatBonuses(this.subjectDef.statBonuses, this.beginTimeUnit, this.timeUnitsSpent);
		}
	}

}
