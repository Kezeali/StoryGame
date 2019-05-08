using UnityEngine;

namespace Cloverview
{

	// Asset type for defining a "stage mark", used to set the positions of characters in event and activity scenes.
	[CreateAssetMenu(fileName="StageMark.asset", menuName="Cloverview/Stage Marking Definition")]
	public class StageMarkData : ScriptableObject, IDataItem
	{
	}

}
