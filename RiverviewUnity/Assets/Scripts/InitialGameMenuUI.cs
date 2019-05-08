using UnityEngine;

namespace Cloverview
{

// UI script for the Start / Continue screen.
public class InitialGameMenuUI : MonoBehaviour, IServiceUser<ProfileSaveData>
{
	public MenuNavigation newButton;
	public GameObject continueButton;

	public void OnEnable()
	{
		App.Register<ProfileSaveData>(this);
	}

	public void OnDisable()
	{
		App.Deregister<ProfileSaveData>(this);
	}

	public void Initialise(ProfileSaveData profileData)
	{
		int savesToLoad = App.instance.saveFilesAvailableForCurrentProfile.Count;
		this.continueButton.SetActive(savesToLoad > 0);
		// If only the new-game option is available, might as well preload the next scene:
		this.newButton.preload = savesToLoad == 0;
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
