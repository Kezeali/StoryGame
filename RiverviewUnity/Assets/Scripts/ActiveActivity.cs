using System.Collections.Generic;

namespace Cloverview
{

	public class ActiveActivity
	{
		public PlanActivityData def;
		public Nav.VisibleEnvScene envScene;
		public Character pc;
		public List<Character> npcs = new List<Character>();
		public int timeUnitsSpent;

		public Character.Status Progress(Character.Status statusInProgress)
		{
			return statusInProgress;
		}
	}

}
