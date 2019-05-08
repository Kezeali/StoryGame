using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Cloverview
{

// UI script for making buttons that transition between menu scenes.
public class MenuNavigation : Clickable, IServiceUser<Nav>, INavigator
{
	[Header("Menu Navigation")]
	public bool preload = true;
	public MenuData destination;
	[ReadOnly]
	public string parentScene;

	Nav nav;

	protected override void OnEnable()
	{
		base.OnEnable();

		if (Application.isPlaying)
		{
			App.Register(this);
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		if (Application.isPlaying)
		{
			App.Deregister(this);
		}
	}

	protected override void OnPress()
	{
		this.Go();
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);
		this.nav = nav;
	}

	public void CompleteInitialisation()
	{
		if (this.destination != null && this.preload && this.destination.allowPreload)
		{
			this.nav.Preload(this.destination, this.parentScene);
		}
	}

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}

	public void Go()
	{
		if (this.destination != null)
		{
			this.nav.GoTo(this.destination, this.parentScene);
		}
	}
}

}
