using UnityEngine;

namespace Cloverview
{

// Asset type for defining the icons used to indicate the the current value of a given stat.
[CreateAssetMenu(fileName="StatIconTable.asset", menuName="Cloverview/Stat Value Icon Table")]
public class StatIconTable : ScriptableObject
{
	[System.Serializable]
	public struct Mapping
	{
		public float minNormalisedValue;
		public float maxNormalisedValue;
		public Sprite icon;
	}

	// TODO: custom editor to auto-sort mappings
	public Mapping[] mappings;

	public Sprite FindIcon(float value)
	{
		Sprite result = null;
		for (int i = 0; i < this.mappings.Length; ++i) {
			Mapping mapping = this.mappings[i];
			if (mapping.minNormalisedValue <= value && value <= mapping.maxNormalisedValue) {
				result = mapping.icon;
				break;
			}
		}
		return result;
	}
}

}