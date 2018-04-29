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
		Across, // Transitioning from another menu in the same scene
	}

	[System.NonSerialized]
	public TransitionState state;

	[System.NonSerialized]
	public MenuData displayedMenu;

	System.Action<Nav.VisibleMenu> transitioningOutCallback;
	Nav.VisibleMenu transitioningOutNavScene;
	int transitionAnimationLayer = -1;
	int transitionAnimationNameHash = 0;

	public void OnEnable()
	{
		this.state = TransitionState.Uninitialised;
		// TODO: only do this if the animator is ready
		this.DetermineTransitionLayerIndex();
		App.Register<SaveData>(this);
	}

	void DetermineTransitionLayerIndex()
	{
		if (this.transitionAnimationLayer == -1 && this.transitionAnimator != null)
		{
			this.transitionAnimationLayer = this.transitionAnimator.GetLayerIndex(this.transitionAnimationLayerName);
		}
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
					this.CompleteTransitionOut();
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

	void CompleteTransitionOut()
	{
		if (this.transitioningOutCallback != null)
		{
			this.transitioningOutCallback(this.transitioningOutNavScene);
			this.transitioningOutCallback = null;
			this.transitioningOutNavScene = null;
		}
		this.state = TransitionState.Idle;
	}

	public void TransitionIn(MenuData sourceMenu, MenuData def)
	{
		this.displayedMenu = def;

		this.DetermineTransitionLayerIndex();

		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;

		this.state = TransitionState.In;
		this.transitionAnimationNameHash = 0;

		if (this.transitionAnimator != null)
		{
			MenuData transitionMenuDef = def.transitionAs != null ? def.transitionAs : def;

			// All menus should have a ResetTransition animation to tidy things up
			this.transitionAnimator.Play("ResetTransition", this.transitionAnimationLayer);
			this.transitionAnimator.Update(0f);

			if (sourceMenu != null)
			{
				// Target menu provided: look for a specific menu-to-menu (across) type transition
				// Animation state name format: In[SourceMenuName]-[DestMenuName]
				MenuData sourceMenuTransitionMenuDef = sourceMenu.transitionAs ?? sourceMenu;
				string transitionAnimationName = Strf.Format("In{0}-{1}", transitionMenuDef.name, sourceMenuTransitionMenuDef.name);

				int hash = Animator.StringToHash(transitionAnimationName);
				if (this.transitionAnimator.HasState(this.transitionAnimationLayer, hash))
				{
					this.transitionAnimationNameHash = hash;
				}

				if (this.transitionAnimationNameHash != 0)
				{
					this.state = TransitionState.Across;
				}
			}

			if (this.transitionAnimationNameHash == 0)
			{
				// There was no menu-to-menu transition found, so look for an in-transition for the new menu
				string transitionAnimationName = transitionMenuDef.name + "In";

				int hash = Animator.StringToHash(transitionAnimationName);
				if (this.transitionAnimator.HasState(this.transitionAnimationLayer, hash))
				{
					this.transitionAnimationNameHash = hash;
				}
			}

			if (this.transitionAnimationNameHash != 0)
			{
				this.transitionAnimator.Play(this.transitionAnimationNameHash, this.transitionAnimationLayer);
			}
		}

		if (this.transitionAnimationNameHash == 0)
		{
			this.state = TransitionState.Idle;
		}
	}

	public void TransitionOut(MenuData destMenu, System.Action<Nav.VisibleMenu> completionCallback = null, Nav.VisibleMenu leavingMenu = null)
	{
		this.DetermineTransitionLayerIndex();

		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = leavingMenu;

		this.displayedMenu = null;

		// Default transition-out state with no animation
		this.state = TransitionState.Out;
		this.transitionAnimationNameHash = 0;

		if (leavingMenu != null)
		{
			MenuData def = leavingMenu.def;

			if (this.transitionAnimator != null)
			{
				MenuData transitionMenuDef = def.transitionAs ?? def;

				// All menus should have a ResetTransition animation to tidy things up
				this.transitionAnimator.Play("ResetTransition", this.transitionAnimationLayer);
				this.transitionAnimator.Update(0f);

				if (destMenu != null)
				{
					// Target menu provided: look for a specific menu-to-menu (across) type transition
					// Animation state name format: Out[SourceMenuName]-[DestMenuName]
					MenuData destMenuTransitionMenuDef = destMenu.transitionAs ?? destMenu;
					string transitionAnimationName = Strf.Format("Out{0}-{1}", transitionMenuDef.name, destMenuTransitionMenuDef.name);

					int hash = Animator.StringToHash(transitionAnimationName);
					if (this.transitionAnimator.HasState(this.transitionAnimationLayer, hash))
					{
						this.transitionAnimationNameHash = hash;
					}

					if (this.transitionAnimationNameHash != 0)
					{
						this.state = TransitionState.Across;
					}
				}

				if (this.transitionAnimationNameHash == 0)
				{
					// There was no menu-to-menu transition found, so look for an out-transition for the current menu
					string transitionAnimationName = transitionMenuDef.name + "Out";

					int hash = Animator.StringToHash(transitionAnimationName);
					if (this.transitionAnimator.HasState(this.transitionAnimationLayer, hash))
					{
						this.transitionAnimationNameHash = hash;
					}
				}

				if (this.transitionAnimationNameHash != 0)
				{
					this.transitionAnimator.Play(this.transitionAnimationNameHash, this.transitionAnimationLayer);
				}
			}
		}

		if (this.transitionAnimationNameHash == 0)
		{
			// Found no transition animation to play
			this.CompleteTransitionOut();
		}
	}
}

}
