using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

public class EnvSceneController : MonoBehaviour, IDataUser<SaveData>, IDataUser<CinemachineBrain>
{
	public Animator sceneAnimator;

	public GameObject[] virtualCameras;

	System.Action<Nav.VisibleEnvScene> transitioningOutCallback;
	Nav.VisibleEnvScene transitioningOutNavScene;

	CinemachineBrain cinemachineBrain;

	public void OnValidate()
	{
	#if UNITY_EDITOR
		for (int cameraIndex = this.virtualCameras.Length-1; cameraIndex >= 0; --cameraIndex)
		{
			if (this.virtualCameras[cameraIndex].GetComponent<ICinemachineCamera>() == null)
			{
				UnityEditor.ArrayUtility.RemoveAt(ref this.virtualCameras, cameraIndex);
			}
		}
	#endif
	}

	public void OnEnable()
	{
		App.Register<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void Initialise(CinemachineBrain cinemachineBrain)
	{
		this.cinemachineBrain = cinemachineBrain;
	}

	public void SetEvent(ActiveEvent @event)
	{
		// TODO(elliot): pass all scene Marks to the event (to the playable director?)

		// TODO(elliot): set time of day?
	}

	public void SetActivity(ActiveActivity activity)
	{
		// TODO(elliot): pass all scene Marks to the activity (to the playable director?)

		// TODO(elliot): set time of day?
	}

	public void ClearActivity()
	{
	}

	public void SetCommuteDirection(CommuteDirection direction)
	{
	}

	public void TransitionIn()
	{
		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;
	}

	public void TransitionOut(System.Action<Nav.VisibleEnvScene> completionCallback = null, Nav.VisibleEnvScene param = null)
	{
		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = param;

		for (int cameraIndex = 0; cameraIndex < this.virtualCameras.Length; ++cameraIndex)
		{
			this.virtualCameras[cameraIndex].SetActive(false);
		}

		// TODO(elliot): hook up to the env camera brain and determine movement velocity
	}
}

}
