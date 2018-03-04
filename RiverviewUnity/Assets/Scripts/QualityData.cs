using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="Quality.asset", menuName="Cloverview/Quality Definition")]
	public class QualityData : ScriptableObject, IDataItem
	{
		public string description;
		public StatBonusData[] statBonuses;
		public MonoBehaviour behaviour;
	}

}