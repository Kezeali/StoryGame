using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public StatBonusData[] statBonuses;
	}

}