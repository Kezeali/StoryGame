using UnityEngine;
using UnityEngine.UI;

using Button = UnityEngine.UI.Button;

namespace Cloverview
{

[System.Flags]
public enum SlotType
{
	None = 0,
	School = 1 << 0,
	FreeTime = 1 << 1,
	Holiday = 1 << 2,
}

// UI script for displaying a slot that can be filled in a plan.
public class PlanSlotUI : MonoBehaviour
{
	public SlotType slotType;

	[SerializeField]
	public PlanOptionUI filledSlotPrefab;

	public Transform content;

	[System.NonSerialized]
	public PlanSlot dataSlot;
	[System.NonSerialized]
	public System.Action<PlanSlotUI> clicked;

	private Button button;

	public void OnEnable()
	{
		this.button = this.GetComponentInChildren<Button>();
		this.button.onClick.AddListener(this.OnClicked);

		if (this.content == null)
		{
			this.content = this.transform.Find("Content");
		}
	}

	// TODO(elliot): make this stuff editor-only, since it is now saved in the schema
	public virtual int GetStartTime()
	{
		return this.transform.GetSiblingIndex();
	}
	// TODO(elliot): make this stuff editor-only, since it is now saved in the schema
	public virtual int GetDuration()
	{
		return 1;
	}

	public void Initialise(PlanSlot dataSlot)
	{
		this.dataSlot = dataSlot;
	}

	public void Display(PlanOption option, PlanOptionUI defaultFilledSlotPrefab)
	{
		this.dataSlot.selectedOption = option;
		this.DisplayCurrent(defaultFilledSlotPrefab);
	}

	public void DisplayCurrent(PlanOptionUI defaultFilledSlotPrefab)
	{
		this.Unpopulate();

		if (this.dataSlot != null && this.dataSlot.selectedOption != null)
		{
			PlanOptionUI prefab = this.filledSlotPrefab ?? defaultFilledSlotPrefab;
			var filledSlotContentInstance = Object.Instantiate(prefab, this.content);
			filledSlotContentInstance.Initialise(this.dataSlot.selectedOption);
		}
	}

	public void Clear()
	{
		this.dataSlot.selectedOption = null;
		this.Unpopulate();
	}

	private void Unpopulate()
	{
		for (int i = this.content.childCount-1; i >= 0; --i)
		{
			Object.Destroy(this.content.GetChild(i).gameObject);
		}
	}

	private void OnClicked()
	{
		if (this.clicked != null)
		{
			this.clicked(this);
		}
	}

	// TODO(elliot): make this method editor only (slot sorting should only need to happen at edit time)
	public static int Compare(PlanSlotUI a, PlanSlotUI b)
	{
		if (a.GetStartTime() < b.GetStartTime())
		{
			return -1;
		}
		else if (a.GetStartTime() > b.GetStartTime())
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}
}

}
