using UnityEngine;
using UnityEngine.UI;


namespace NotABear
{

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
		Refresh();
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