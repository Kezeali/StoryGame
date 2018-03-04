using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity Definition")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public StatBonusData[] statBonuses;
	}

}