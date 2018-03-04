using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="Stat.asset", menuName="Cloverview/Character Stat Definition")]
	public class CharacterStatDefinition : ScriptableObject, IDataItem
	{
		public string title;
		public string valueFormat;
		public float baseValue;
		public float minValue;
		public float maxValue;

		public void Reset()
		{
			this.title = this.name;
			this.valueFormat = "F2";
			this.baseValue = 1;
			this.minValue = 1;
			this.maxValue = 100;
		}
	}

}