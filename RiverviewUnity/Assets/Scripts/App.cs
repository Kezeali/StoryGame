using UnityEngine;
using NotABear;
using System.Collections.Generic;

public class App : MonoBehaviour
{
	[SerializeField]
	private PlannerData plannerData;

	DataItemSource dataItemSource;

	PlanOptionsLoadout loadout;
	Plan plan;

	public void Awake()
	{
		dataItemSource = new DataItemSource();
		dataItemSource.AddDataItemRange(plannerData.items);

		loadout = new PlanOptionsLoadout();
		loadout.name = "test";

		loadout.planOptions = new List<PlanOption>(plannerData.items.Length);

		for (int i = 0; i < plannerData.items.Length; ++i)
		{
			PlannerItemData item = plannerData.items[i];
			PlanOption option = new PlanOption();
			option.data = item;
			loadout.planOptions.Add(option);
		}

		plan = new Plan();
		plan.name = "WeekPlan";
		Serialiser.Deserialise(ref plan, dataItemSource);
	}

	public void Start()
	{
		Object[] planOptionSelectorObjects = Object.FindObjectsOfType(typeof(PlanOptionSelectorUI));
		for (int i = 0; i < planOptionSelectorObjects.Length; ++i)
		{
			var planOptionSelectorUI = planOptionSelectorObjects[i] as PlanOptionSelectorUI;
			planOptionSelectorUI.Initialise(loadout);
		}

		Object[] planObjects = Object.FindObjectsOfType(typeof(PlanUI));
		for (int i = 0; i < planObjects.Length; ++i)
		{
			var planUI = planObjects[i] as PlanUI;
			planUI.Initialise(plan, dataItemSource);
		}
	}
}
