using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

public class MenuSceneController : MonoBehaviour, IDataUser<SaveData>
{
	public Animator transitionAnimator;

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

	int transitionAnimationStateId;

	public void OnEnable()
	{
		this.state = TransitionState.Uninitialised;
		App.Register<SaveData>(this);
	}

	public void Initialise(SaveData saveData)
	{
	}

	public void Update()
	{
		switch (this.state)
		{
			case TransitionState.Out:
			{
				if (this.transitionAnimator != null && this.transitionAnimator)
				{
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
				if (this.transitionAnimator != null && this.transitionAnimator)
				{
				}
				else
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

		this.transitionAnimationStateId = Animator.StringToHash(def.name);
	}

	public void TransitionOut(System.Action<Nav.VisibleMenu> completionCallback = null, Nav.VisibleMenu param = null)
	{
		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = param;

		this.state = TransitionState.Out;
	}
}

}
