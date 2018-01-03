using UnityEngine;
using NotABear;

public class PlanOptionUI : MonoBehaviour
{
	public PlanOption planOption;

	public System.Action<PlanOptionUI> Selected;

	public void Initialise(PlanOption option)
	{
		this.planOption = option;
	}

	public void Clicked()
	{
		if (Selected != null)
		{
			Selected(this);
		}
	}
}
