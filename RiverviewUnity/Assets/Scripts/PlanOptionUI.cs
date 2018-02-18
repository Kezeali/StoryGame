using UnityEngine;
using UnityEngine.UI;
using NotABear;

public class PlanOptionUI : MonoBehaviour
{
	[SerializeField]
	private Text labelText;

	[SerializeField]
	private Image icon;

	private Selectable clickableArea;

	public PlanOption planOption;

	public System.Action<PlanOptionUI> Selected;

	public void Awake()
	{
		this.clickableArea = this.GetComponentInChildren<Selectable>();
	}

	public void Initialise(PlanOption option)
	{
		this.planOption = option;
		this.labelText.text = option.plannerItem.name;
	}

	public void EnableSelection()
	{
		this.clickableArea.interactable = false;
	}

	public void DisableSelection()
	{
		this.clickableArea.interactable = true;
	}

	public void Clicked()
	{
		if (Selected != null)
		{
			Selected(this);
		}
	}
}
