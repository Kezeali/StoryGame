using UnityEngine;
using NotABear;
using System.Collections.Generic;

public class App : MonoBehaviour
{
	[SerializeField]
	private PlannerData plannerData;

	public void Awake()
	{
		PlanOptionsLoadout loadout = new PlanOptionsLoadout();
		loadout.name = "test";

		loadout.planOptions = new List<PlanOption>(plannerData.items.Length);

		for (int i = 0; i < plannerData.items.Length; ++i)
		{
			PlannerItemData item = plannerData.items[i];
			PlanOption option = new PlanOption();
			option.data = item;
			loadout.planOptions.Add(option);
		}

		Object[] planOptionSelectorObjects = Object.FindObjectsOfType(typeof(PlanOptionSelectorUI));
		for (int i = 0; i < planOptionSelectorObjects.Length; ++i)
		{
			PlanOptionSelectorUI planOptionSelectorUI = planOptionSelectorObjects[i] as PlanOptionSelectorUI;
			planOptionSelectorUI.Initialise(loadout);
		}
	}
}
