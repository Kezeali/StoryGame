using UnityEngine;

namespace Cloverview
{

public class InitialGameMenuUI : MonoBehaviour
{
	public void HandleNewGameClicked()
	{
		App.instance.NewGame();
	}

	public void HandleContinueClicked()
	{
		App.instance.LoadGame();
	}
}

}
