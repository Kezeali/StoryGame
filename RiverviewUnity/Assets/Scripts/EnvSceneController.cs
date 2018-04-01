using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

public class EnvSceneController : MonoBehaviour, IDataUser<SaveData>
{
	public Animator sceneAnimator;

	public CinemachineTargetGroup targetGroup;

	public GameObject[] virtualCameras;

	public Rigidbody[] physicalObjects;

	public enum TransitionState
	{
		Uninitialised,
		Idle,
		In,
		Out,
	}

	[System.NonSerialized]
	public TransitionState state;

	System.Action<Nav.VisibleEnvScene> transitioningOutCallback;
	Nav.VisibleEnvScene transitioningOutNavScene;

	CinemachineBrain cinemachineBrain;

	Animator globalTransitionAnimator;

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
		this.state = TransitionState.Uninitialised;
		App.Register<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void SetCamera(CinemachineBrain cinemachineBrain)
	{
		this.cinemachineBrain = cinemachineBrain;
		this.state = TransitionState.Idle;
	}

	public void SetGlobalTransitionAnimator(Animator globalTransitionAnimator)
	{
		this.globalTransitionAnimator = globalTransitionAnimator;
	}

	public void SetEvent(ActiveEvent @event)
	{
		// TODO(elliot): pass all scene Marks to the event (to the playable director?)

		// TODO(elliot): set time of day?
	}

	public void ClearEvent()
	{
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

	public void Update()
	{
		switch (this.state)
		{
			case TransitionState.Out:
			{
				if (this.cinemachineBrain != null && this.cinemachineBrain.IsBlending)
				{
					this.ApplyPhysics();
				}
				else
				{
					if (this.transitioningOutCallback != null)
					{
						this.transitioningOutCallback(this.transitioningOutNavScene);
						this.transitioningOutCallback = null;
						this.transitioningOutNavScene = null;
					}
					this.state = TransitionState.Idle;
				}
			} break;
			case TransitionState.In:
			{
				if (this.cinemachineBrain != null && this.cinemachineBrain.IsBlending)
				{
					this.ApplyPhysics();
				}
				else
				{
					this.state = TransitionState.Idle;
				}
			} break;
		}
	}

	private void ApplyPhysics()
	{
	}

	public void TransitionIn()
	{
		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;

		for (int cameraIndex = 0; cameraIndex < this.virtualCameras.Length; ++cameraIndex)
		{
			this.virtualCameras[cameraIndex].SetActive(true);
		}

		this.state = TransitionState.In;
	}

	public void TransitionOut(System.Action<Nav.VisibleEnvScene> completionCallback = null, Nav.VisibleEnvScene param = null)
	{
		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = param;

		for (int cameraIndex = 0; cameraIndex < this.virtualCameras.Length; ++cameraIndex)
		{
			this.virtualCameras[cameraIndex].SetActive(false);
		}

		this.state = TransitionState.Out;
	}
}

}
