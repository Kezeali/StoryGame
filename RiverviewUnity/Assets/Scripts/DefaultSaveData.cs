using UnityEngine;
using SubjectNerd.Utilities;

namespace Cloverview
{

	[CreateAssetMenu(fileName="CustomSave.asset", menuName="Cloverview/Custom Save Game")]
	public class DefaultSaveData : ScriptableObject
	{
		public SaveData saveData;
	}

}
