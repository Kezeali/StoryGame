using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="Subject.asset", menuName="Cloverview/Subject Definition")]
	public class SubjectData : ScriptableObject, IDataItem
	{
		public string title;
		public QualityData[] requiredQualityTags;
		public StatBonusData[] statBonuses;
		public StatAffectorData[] statAffectors;
	}

}