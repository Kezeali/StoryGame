using UnityEngine;

namespace Cloverview
{

	// Asset type to define a character "quality". See also Character.
	[CreateAssetMenu(fileName="Quality.asset", menuName="Cloverview/Quality Definition")]
	public class QualityData : ScriptableObject, IDataItem
	{
		public string description;
		public StatBonusData[] statBonuses;
		public StatAffectorData[] statAffectors;
	}

}