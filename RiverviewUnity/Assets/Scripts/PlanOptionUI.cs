using UnityEngine;
using UnityEngine.UI;
using NotABear;

public class PlanOptionUI : MonoBehaviour
{
	[SerializeField]
	private Text labelText;

	[SerializeField]
	private Image icon;

	public PlanOption planOption;

	public System.Action<PlanOptionUI> Selected;

	public void Initialise(PlanOption option)
	{
		this.planOption = option;
		labelText.text = option.data.name;
	}

	public void Clicked()
	{
		if (Selected != null)
		{
			Selected(this);
		}
	}
}
