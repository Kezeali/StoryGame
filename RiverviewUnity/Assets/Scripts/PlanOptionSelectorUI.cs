using UnityEngine;
using System.Collections.Generic;
using Cloverview;

// UI script for displaying plan options for a selected slot
public class PlanOptionSelectorUI : MonoBehaviour, IServiceUser<PlannerDataIndex>, IServiceUser<SaveData>
{
	[SerializeField]
	private Transform optionsContainer;

	[SerializeField]
	private PlanOptionUI optionUIPrefab;

	[SerializeField]
	private PlanOptionCategoryUI optionCategoryUIPrefab;

	PlannerDataIndex plannerData;
	SaveData saveData;
	//PlanOptionSelectorState selectorSaveData;

	List<PlanOptionUI> optionUis = new List<PlanOptionUI>();
	List<PlanOptionCategoryUI> optionCategoryUis = new List<PlanOptionCategoryUI>();

	PlanOptionsLoadout currentLoadout;
	PlanOptionsLoadout rootLoadout;

	PlanUI activePlanUI;

	public void OnEnable()
	{
		App.Register<PlannerDataIndex>(this);
		App.Register<SaveData>(this);
	}

	public void OnDisabled()
	{
		App.Deregister<PlannerDataIndex>(this);
		App.Deregister<SaveData>(this);
	}

	public void Initialise(PlannerDataIndex plannerData)
	{
		this.plannerData = plannerData;
	}

	public void Initialise(SaveData saveData)
	{
		this.saveData = saveData;
	}

	public void CompleteInitialisation()
	{
		Debug.Assert(this.optionsContainer != null);

		this.HideAll();

		this.rootLoadout = this.MakeRootLoadout();
	}

	public void Populate(PlanUI activePlanUI, PlanSlotUI slot)
	{
		this.activePlanUI = activePlanUI;

		this.HideAll();

		SlotType type = slot.slotType;

		for (int i = 0; i < this.saveData.planOptionSelectorStates.Count; ++i) {
			if (this.saveData.planOptionSelectorStates[i].name == this.activePlanUI.planSchema.name) {
				//this.selectorSaveData = this.saveData.planOptionSelectorStates[i];
				break;
			}
		}

		this.currentLoadout = this.MakeDerrivedLoadout(this.rootLoadout, type);

		for (int i = 0; i < currentLoadout.planOptions.Count; ++i) {
			Transform container;
			List<PlanOptionUI> uiCollection;

			PlanOption option = this.currentLoadout.planOptions[i];
			if (option.plannerItem.subject != null) {
				PlanOptionCategoryUI categoryUi = this.GetCategoryUI(option.plannerItem.subject);
				if (categoryUi == null) {
					categoryUi = Object.Instantiate(this.optionCategoryUIPrefab, this.optionsContainer);

					categoryUi.subject = option.plannerItem.subject;
					categoryUi.title.text = categoryUi.subject.title;

					this.optionCategoryUis.Add(categoryUi);
				}
				container = categoryUi.optionsContainer;
				uiCollection = categoryUi.optionUis;
			} else {
				container = this.optionsContainer;
				uiCollection = this.optionUis;
			}

			PlanOptionUI newUI = Object.Instantiate(this.optionUIPrefab, container);

			newUI.Initialise(option);
			newUI.Selected += SelectOption;

			uiCollection.Add(newUI);
		}
	}

	private PlanOptionCategoryUI GetCategoryUI(SubjectData subjectData)
	{
		PlanOptionCategoryUI result = null;
		for (int i = 0; i < this.optionCategoryUis.Count; ++i)
		{
			if (this.optionCategoryUis[i].subject == subjectData)
			{
				result = this.optionCategoryUis[i];
			}
		}
		return result;
	}

	public PlanOptionsLoadout MakeDerrivedLoadout(PlanOptionsLoadout rootLoadout, SlotType type)
	{
		var newLoadout = new PlanOptionsLoadout();
		newLoadout.name = type.ToString();

		newLoadout.planOptions = new List<PlanOption>(rootLoadout.planOptions.Count);

		for (int i = 0; i < rootLoadout.planOptions.Count; ++i)
		{
			PlanOption option = rootLoadout.planOptions[i];
			if ((option.plannerItem.validSlots & type) != 0)
			{
				newLoadout.planOptions.Add(option);
			}
		}
		return newLoadout;
	}

	public PlanOptionsLoadout MakeRootLoadout()
	{
		var newLoadout = new PlanOptionsLoadout();
		newLoadout.name = "root";

		newLoadout.planOptions = new List<PlanOption>(this.plannerData.items.Length);

		for (int i = 0; i < this.plannerData.items.Length; ++i)
		{
			PlannerItemData item = this.plannerData.items[i];
			{
				PlanOption option = new PlanOption();
				option.plannerItem = item;
				newLoadout.planOptions.Add(option);
			}
		}
		return newLoadout;
	}

	public void HideAll()
	{
		// TODO: hide rather than destroy
		for (int i = 0; i < this.optionUis.Count; ++i)
		{
			Object.Destroy(this.optionUis[i].gameObject);
		}
		this.optionUis.Clear();

		for (int i = 0; i < this.optionCategoryUis.Count; ++i)
		{
			this.optionCategoryUis[i].Clear();
			Object.Destroy(this.optionCategoryUis[i].gameObject);
		}
		this.optionCategoryUis.Clear();
	}

	public void DeselectOption(PlanOption option)
	{
	}

	public void SelectOption(PlanOptionUI option)
	{
		activePlanUI.SelectOption(option);
	}
}
