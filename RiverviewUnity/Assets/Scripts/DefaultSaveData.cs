using UnityEngine;
using SubjectNerd.Utilities;

namespace NotABear
{

	[CreateAssetMenu(fileName="DefaultSave.asset", menuName="Cloverview/Default Save")]
	public class DefaultSaveData : ScriptableObject
	{
		public SaveData saveData;
	}

}
