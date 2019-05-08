using UnityEngine;

namespace Cloverview
{

	public enum StatVisibility
	{
		Nowhere = 0,
		StatsPage = 1 << 0,
	}

	public enum StatValueDisplay
	{
		NumericValue = 1 << 0,
		IconicValue = 1 << 1,
	}

	// Data asset type for character stats. Has information about parameters of the stat, and how it can be displayed.
	[CreateAssetMenu(fileName="Stat.asset", menuName="Cloverview/Character Stat Definition")]
	public class CharacterStatDefinition : ScriptableObject, IDataItem
	{
		public string title;
		public Sprite icon;
		public string valueFormat;
		public StatIconTable valueIcons;
		public float baseValue;
		public float minValue;
		public float maxValue;
		public AnimationCurve baseDecayCurve;
		[EnumFlag]
		public StatVisibility visibility;
		[EnumFlag]
		public StatValueDisplay allowedDisplayMode;

		public void Awake()
		{
			this.baseValue = Mathf.Clamp(this.baseValue, this.minValue, this.maxValue);
		}

		public void Reset()
		{
			this.title = this.name;
			this.valueFormat = "F2";
			this.baseValue = 1;
			this.minValue = 1;
			this.maxValue = 100;
			this.baseDecayCurve = AnimationCurve.Constant(0, 1, 0);
			this.visibility = StatVisibility.StatsPage;
		}
	}

}