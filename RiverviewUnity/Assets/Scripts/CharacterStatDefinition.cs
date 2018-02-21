using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="Stat.asset", menuName="Riverview/Character Stat")]
	public class CharacterStatDefinition : ScriptableObject, IDataItem
	{
		public string title;
		public double baseValue;
		public double minValue;
		public double maxValue;

		public void Reset()
		{
			this.title = this.name;
			this.baseValue = 1;
			this.minValue = 1;
			this.maxValue = 100;
		}
	}

}