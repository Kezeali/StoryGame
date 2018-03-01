using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace NotABear
{

public class MenuNavigation : Clickable, IDataUser<Nav>
{
	[Header("Menu Navigation")]
	public bool preload = true;
	public MenuData destination;

	Nav nav;

	protected override void Awake()
	{
		base.Awake();

		if (Application.isPlaying)
		{
			App.Register(this);
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

		if (this.preload && this.destination.allowPreload)
		{
			this.nav.Preload(this.destination);
		}
	}

	public void Go()
	{
		this.nav.GoTo(this.destination);
	}
}

}
