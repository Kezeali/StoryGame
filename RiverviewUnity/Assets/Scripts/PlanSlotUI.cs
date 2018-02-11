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

	private PlanUI planUI;
	private Button button;
	[System.NonSerialized]
	public PlanSlot dataSlot;

	public void Awake()
	{
		this.button = this.GetComponentInChildren<Button>();
		this.button.onClick.AddListener(this.Clicked);

		this.content = this.transform.Find("Content");
	}

	public virtual int SlotUnitIndex()
	{
		return this.transform.GetSiblingIndex();
	}

	public void Initialise(PlanUI planUI, PlanSlot dataSlot)
	{
		this.planUI = planUI;
		this.dataSlot = dataSlot;
	}

	public void Fill(PlanOption option)
	{
		this.dataSlot.selectedOption = option;
	}

	public void Clear()
	{
		this.dataSlot.selectedOption = null;
	}

	public void Clicked()
	{
		this.planUI.OnSlotClicked(this);
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
