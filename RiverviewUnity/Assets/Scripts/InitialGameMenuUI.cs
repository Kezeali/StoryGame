using UnityEngine;

namespace Cloverview
{

public class InitialGameMenuUI : MonoBehaviour, IServiceUser<ProfileSaveData>
{
	public GameObject continueButton;

	public void OnEnable()
	{
		App.Register<ProfileSaveData>(this);
	}

	public void Initialise(ProfileSaveData profileData)
	{
		int savesToLoad = App.instance.saveFilesAvailableForCurrentProfile.Count;
		this.continueButton.SetActive(savesToLoad > 0);
	}

	public void CompleteInitialisation()
	{
	}

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
