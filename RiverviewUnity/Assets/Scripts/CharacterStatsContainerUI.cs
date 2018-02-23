using UnityEngine;
using NotABear;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NotABear
{

public class CharacterStatsContainerUI : MonoBehaviour, ISaveDataUser
{
	private Character character;
	private CharacterStatUI[] statUIs;

	public void Awake()
	{
		this.statUIs = this.GetComponentsInChildren<CharacterStatUI>();

		App.Register(this);
	}

	public void Initialise(SaveData loadedData)
	{
	}
}

}
