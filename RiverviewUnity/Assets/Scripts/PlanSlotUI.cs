using UnityEngine;
using UnityEngine.UI;

namespace NotABear
{

[System.Flags]
public enum SlotType
{
	None = 0,
	School = 1 << 0,
	FreeTime = 1 << 1,
	Holiday = 1 << 2,
}

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

	public void Awake()
	{
		this.button = this.GetComponentInChildren<Button>();
		this.button.onClick.AddListener(this.OnClicked);

		this.content = this.transform.Find("Content");
	}

	public virtual int SlotUnitIndex()
	{
		return this.transform.GetSiblingIndex();
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
		Unpopulate();

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
		Unpopulate();
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

	public static int Compare(PlanSlotUI a, PlanSlotUI b)
	{
		if (a.SlotUnitIndex() < b.SlotUnitIndex())
		{
			return -1;
		}
		else if (a.SlotUnitIndex() > b.SlotUnitIndex())
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
