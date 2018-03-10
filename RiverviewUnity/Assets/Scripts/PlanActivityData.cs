using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity Definition")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public SceneData scene;
		public StatBonusData[] statBonuses;
	}

}