using UnityEngine;
using UnityEngine.UI;


namespace Cloverview
{

// A UI script for containing an individual player stat.
public class CharacterStatUI : MonoBehaviour
{
	[SerializeField]
	private Text labelText;
	[SerializeField]
	private Text valueText;

	[SerializeField]
	private Image icon;

	public CharacterStatDefinition statType;
	[System.NonSerialized]
	public Character character;

	public void Initialise(Character character, CharacterStatDefinition statType = null)
	{
		this.character = character;
		if (statType != null)
		{
			this.statType = statType;
		}
		this.Refresh();
		this.InvokeRepeating("Refresh", 1, 0.5f);
	}

	public void Refresh()
	{
		if (this.character != null && this.statType != null)
		{
			Character.Stat stat = this.character.status.GetStat(this.statType);

			this.labelText.text = this.statType.title;
			this.valueText.text = stat.value.ToString(this.statType.valueFormat);
		}
	}
}

}
