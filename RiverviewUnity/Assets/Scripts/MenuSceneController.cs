using UnityEngine;
using Cloverview;
using Cinemachine;
using System.Collections.Generic;

namespace Cloverview
{

[ExecutionOrder(10)]
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
	bool completedAttemptToPlay;
	MenuData previousMenu;

#if UNITY_EDITOR
	// As of 2018.1.4 scene processors run too late when playing in editor so OnEnabled gets called before ProcessSceneToAllowPreload runs on newly loaded scenes
	bool hasBeenDisabled;
#endif

	public void OnEnable()
	{
		this.state = TransitionState.Uninitialised;
		// TODO: only do this if the animator is ready
		this.DetermineTransitionLayerIndex();

		App.Register<SaveData>(this);

	#if UNITY_EDITOR
		if (this.hasBeenDisabled) {
	#endif
		// NOTE(elliot): the scenecontroller script is set to execute late (see the ExecutionOrder attribute above) so all other service users in the scene should be registered by now
		App.instance.Initialise();
	#if UNITY_EDITOR
		}
	#endif
	}

	public void OnDisable()
	{
		App.Deregister<SaveData>(this);

	#if UNITY_EDITOR
		this.hasBeenDisabled = true;
	#endif
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
					if (!this.completedAttemptToPlay) {
						this.AttemptToPlayTransitionIn(this.previousMenu, this.displayedMenu);
					}
					AnimatorStateInfo stateInfo = this.transitionAnimator.GetCurrentAnimatorStateInfo(this.transitionAnimationLayer);
					if (stateInfo.shortNameHash == this.transitionAnimationNameHash && stateInfo.normalizedTime < 1.0f)
					{
						stillGoing = true;
					}
				}
				if (!stillGoing)
				{
					this.previousMenu = null;
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

	public void TransitionIn(MenuData sourceMenu, MenuData destMenu)
	{
		this.previousMenu = sourceMenu;
		this.displayedMenu = destMenu;

		this.DetermineTransitionLayerIndex();

		this.transitioningOutCallback = null;
		this.transitioningOutNavScene = null;

		this.state = TransitionState.In;
		this.transitionAnimationNameHash = 0;

		this.AttemptToPlayTransitionIn(sourceMenu, destMenu);
	}

	void AttemptToPlayTransitionIn(MenuData sourceMenu, MenuData destMenu)
	{
		bool ready = true;
		if (this.transitionAnimator != null && this.transitionAnimator.runtimeAnimatorController != null)
		{
			if (this.transitionAnimator.isInitialized)
			{
				MenuData transitionMenuDef = destMenu.transitionAs ?? destMenu;

				Debug.LogFormat("MenuSceneController: Playing transition animation on {0}, entering {1} from {2}", this.transitionAnimator.runtimeAnimatorController.name, destMenu.name, sourceMenu != null ? sourceMenu.name : "null");

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
					Debug.Log("MenuSceneController: Playing menu-in transition");
					this.transitionAnimator.Play(this.transitionAnimationNameHash, this.transitionAnimationLayer);
				}
			}
			else
			{
				// Animator is not initlialized yet
				ready = false;
			}
		}

		if (ready && this.transitionAnimationNameHash == 0)
		{
			this.state = TransitionState.Idle;
		}

		this.completedAttemptToPlay = ready;
	}

	public void TransitionOut(MenuData destMenu, System.Action<Nav.VisibleMenu> completionCallback = null, Nav.VisibleMenu sourceNavScene = null)
	{
		this.DetermineTransitionLayerIndex();

		this.transitioningOutCallback = completionCallback;
		this.transitioningOutNavScene = sourceNavScene;

		this.displayedMenu = null;

		// Default transition-out state with no animation
		this.state = TransitionState.Out;
		this.transitionAnimationNameHash = 0;

		if (sourceNavScene != null)
		{
			MenuData sourceMenu = sourceNavScene.def;

			Debug.Log("MenuSceneController: Transitioning out of " + sourceMenu.name);

			// NOTE(elliot): the isInitialized check here means that if this animator isn't initialized yet the transition-out animation will not play: this is desired behaviour, as it means the scene is being left immediately after loading, so it's fine to just go straight to the next scene.
			if (this.transitionAnimator != null && this.transitionAnimator.runtimeAnimatorController != null && this.transitionAnimator.isInitialized)
			{
				MenuData transitionMenuDef = sourceMenu.transitionAs ?? sourceMenu;

				Debug.LogFormat("MenuSceneController: Playing transition animation on {0}, leaving {1} to enter {2}", this.transitionAnimator.runtimeAnimatorController.name, sourceMenu.name, destMenu != null ? destMenu.name : "null");

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
