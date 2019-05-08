using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

	// Just an asset wrapper so save games can be referenced in the editor, whever that's useful.
	[CreateAssetMenu(fileName="CustomSave.asset", menuName="Cloverview/Custom Save Game")]
	public class DefaultSaveData : ScriptableObject
	{
		public SaveData saveData;
	}

}
