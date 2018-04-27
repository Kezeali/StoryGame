using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

public class MenuSceneController : MonoBehaviour, IServiceUser<SaveData>
{
	public Animator transitionAnimator;
	public string transitionAnimationLayerName = "Transitions";

	public enum TransitionState
	{
		Uninitialised,
		Idle,
		In,
		Out,
	}

	[System.NonSerialized]
	public TransitionState state;

	System.Action<Nav.VisibleMenu> transitioningOutCallback;
	Nav.VisibleMenu transitioningOutNavScene;
	int transitionAnimationLayer = -1;
	int transitionAnimationNameHash = 0;

	public void OnEnable()
	{
		this.state = TransitionState.Uninitialised;
		if (this.transitionAnimationLayer == -1 && this.transitionAnimator != null)
		{
			this.transitionAnimationLayer = this.transitionAnimator.GetLayerIndex(this.transitionAnimationLayerName);
		}
		App.Register<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void CompleteInitialisation()
	{
	}

	public void Update()
	{
		switch (this.state)
		{
			case TransitionState.Out:
			{
				bool stillGoing = false;
				if (this.transitionAnimator != null)
				{
					AnimatorStateInfo stateInfo = this.transitionAnimator.GetCurrentAnimatorStateInfo(this.transitionAnimationLayer);
					if (stateInfo.shortNameHash == this.transitionAnimationNameHash && stateInfo.normalizedTime < 1.0f)
					{
						stillGoing = true;
					}
				}
				if (!stillGoing)
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
				bool stillGoing = false;
				if (this.transitionAnimator != null)
				{
					AnimatorStateInfo stateInfo = this.transitionAnimator.GetCurrentAnimatorStateInfo(this.transitionAnimationLayer);
					if (stateInfo.shortNameHash == this.transitionAnimationNameHash && stateInfo.normalizedTime < 1.0f)
					{
						stillGoing = true;
					}
				}
				if (!stillGoing)
				{
					this.state = TransitionState.Idle;
				}
			} break;
		}
	}

	public void TransitionIn(MenuData def)
	{
		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;

		this.state = TransitionState.In;

		if (this.transitionAnimator != null)
		{
			MenuData transitionMenuDef = def.transitionAs != null ? def.transitionAs : def;
			string transitionAnimationName = transitionMenuDef.name + "In";

			this.transitionAnimationNameHash = Animator.StringToHash(transitionAnimationName);

			this.transitionAnimator.Play("ResetTransition", this.transitionAnimationLayer);
			this.transitionAnimator.Update(0f);
			this.transitionAnimator.Play(this.transitionAnimationNameHash, this.transitionAnimationLayer);
		}
	}

	public void TransitionOut(System.Action<Nav.VisibleMenu> completionCallback = null, Nav.VisibleMenu param = null)
	{
		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = param;

		this.state = TransitionState.Out;

		if (param != null)
		{
			MenuData def = param.def;

			if (this.transitionAnimator != null)
			{
				MenuData transitionMenuDef = def.transitionAs != null ? def.transitionAs : def;
				string transitionAnimationName = transitionMenuDef.name + "Out";

				this.transitionAnimationNameHash = Animator.StringToHash(transitionAnimationName);

				this.transitionAnimator.Play("ResetTransition", this.transitionAnimationLayer);
				this.transitionAnimator.Update(0f);
				this.transitionAnimator.Play(this.transitionAnimationNameHash, this.transitionAnimationLayer);
			}
		}
	}
}

}
