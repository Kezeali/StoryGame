using UnityEngine;
using NotABear;
using System.Collections.Generic;

namespace NotABear
{

public class CharacterStatsContainerUI : MonoBehaviour, IDataUser<SaveData>
{
	[SerializeField]
	private CharacterStatUI statUiPrefab;

	[SerializeField]
	private Transform additionalStatsContainer;

	private Character character;
	private CharacterStatUI[] statUIs;

	public void OnEnable()
	{
		this.statUIs = this.GetComponentsInChildren<CharacterStatUI>();
		App.Register(this);
	}

	public void Initialise(SaveData loadedData)
	{
		this.character = loadedData.pc;

		Debug.Assert(this.character != null);
		
		for (int i = 0; i < this.statUIs.Length; ++i)
		{
			Debug.Assert(this.statUIs[i].statType != null);
			this.statUIs[i].Initialise(this.character);
		}

		if (this.additionalStatsContainer != null)
		{
			Character.Status status = this.character.status;
			for (int i = 0; i < status.stats.Count; ++i)
			{
				Character.Stat stat = status.stats[i];
				CharacterStatUI statUi = this.GetStatUI(stat.definition);
				if (statUi == null)
				{
					statUi = Object.Instantiate(this.statUiPrefab, this.additionalStatsContainer);
					statUi.Initialise(this.character, stat.definition);
				}
			}
		}
	}

	CharacterStatUI GetStatUI(CharacterStatDefinition statDef)
	{
		CharacterStatUI result = null;
		for (int i = 0; i < this.statUIs.Length; ++i)
		{
			CharacterStatUI statUi = this.statUIs[i];
			if (statUi.statType == statDef)
			{
				result = statUi;
				break;
			}
		}
		return result;
	}
}

}