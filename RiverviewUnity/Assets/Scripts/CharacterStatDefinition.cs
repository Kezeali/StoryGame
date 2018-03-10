using UnityEngine;

namespace Cloverview
{

	public enum StatVisibility
	{
		Nowhere = 0,
		StatsPage = 1 << 0,
	}

	[CreateAssetMenu(fileName="Stat.asset", menuName="Cloverview/Character Stat Definition")]
	public class CharacterStatDefinition : ScriptableObject, IDataItem
	{
		public string title;
		public string valueFormat;
		public float baseValue;
		public float minValue;
		public float maxValue;
		[EnumFlag]
		public StatVisibility visibility;

		public void Reset()
		{
			this.title = this.name;
			this.valueFormat = "F2";
			this.baseValue = 1;
			this.minValue = 1;
			this.maxValue = 100;
			this.visibility = StatVisibility.StatsPage;
		}
	}

}