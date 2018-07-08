using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

[ExecutionOrder(10)]
public class EnvSceneController : MonoBehaviour, IServiceUser<SaveData>
{
	public Animator sceneAnimator;

	public CinemachineTargetGroup targetGroup;

	public GameObject[] virtualCameras;

	public Rigidbody[] physicalObjects;

	[System.Serializable]
	public struct StageMarking
	{
		public StageMarkData def;
		public Transform location;
	}
	public StageMarking[] stageMarkings;

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

	Animator genericTransitionAnimator;
	
	SceneData sceneDef;
	Animator transitionAnimator;
	int transitionLayerIndex;
	int transitionAnimationStateId;

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

		// NOTE(elliot): the scenecontroller script is set to execute late (see the ExecutionOrder attribute above) so all other service users in the scene should be registered by now
		App.instance.Initialise();
	}

	public void OnDisable()
	{
		App.Deregister<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void CompleteInitialisation()
	{
	}

	public void SetCamera(CinemachineBrain cinemachineBrain)
	{
		this.cinemachineBrain = cinemachineBrain;
		this.state = TransitionState.Idle;
	}

	public void SetGlobalTransitionAnimator(Animator genericTransitionAnimator)
	{
		this.genericTransitionAnimator = genericTransitionAnimator;
	}

	public void SetEvent(ActiveEvent @event)
	{
		// TODO(elliot): pass all scene Marks to the event (which will have the Playable Director?)

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

	public void TransitionIn(SceneData def)
	{
		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;

		for (int cameraIndex = 0; cameraIndex < this.virtualCameras.Length; ++cameraIndex)
		{
			this.virtualCameras[cameraIndex].SetActive(true);
		}

		string transitionAnimationName = string.IsNullOrEmpty(def.transitionInNameOverride) ? def.name : def.transitionInNameOverride;

		this.transitionAnimator = null;
		switch (def.transitionIn)
		{
			case SceneTransitionType.Generic:
				this.transitionAnimator = this.genericTransitionAnimator;
				break;
			case SceneTransitionType.SceneController:
				this.transitionAnimator = this.sceneAnimator;
				break;
		}

		if (this.transitionAnimator != null)
		{
			this.transitionAnimator.Play("ResetTransition");
			this.transitionAnimator.Update(0f);
			this.transitionAnimator.Play(transitionAnimationName);
		}

		this.sceneDef = def;

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

		if (this.sceneDef != null)
		{
			SceneData def = this.sceneDef;
			string transitionAnimationName = string.IsNullOrEmpty(def.transitionOutNameOverride) ? def.name : def.transitionOutNameOverride;

			switch (def.transitionOut)
			{
				case SceneTransitionType.Generic:
					this.transitionAnimator = this.genericTransitionAnimator;
					break;
				case SceneTransitionType.SceneController:
					this.transitionAnimator = this.sceneAnimator;
					break;
			}

			if (this.transitionAnimator != null)
			{
				this.transitionAnimator.Play("ResetTransition");
				this.transitionAnimator.Update(0f);
				this.transitionAnimator.Play(transitionAnimationName);
			}
		}

		this.state = TransitionState.Out;
	}
}

}
