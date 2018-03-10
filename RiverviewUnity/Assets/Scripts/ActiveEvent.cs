using System.Collections.Generic;

namespace Cloverview
{

	public class ActiveEvent
	{
		public PlanActivityData def;
		public Nav.VisibleEnvScene envScene;
		public Character pc;
		public List<Character> npcs = new List<Character>();

		public Character.Status Progress(Character.Status statusInProgress)
		{
			return statusInProgress;
		}
	}

}
