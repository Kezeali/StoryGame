using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="StageMark.asset", menuName="Cloverview/Stage Marking Definition")]
	public class StageMarkData : ScriptableObject, IDataItem
	{
		public string title;
		
	}

}
