using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Button = NotABear.Button;

namespace NotABear
{

public class MenuNavigation : Button, IDataUser<Nav>
{
	public bool preload = true;
	public MenuData destination;

	Nav nav;

	protected override void Awake()
	{
		base.Awake();

		App.Register(this);
	}

	public void Initialise(Nav nav)
	{
		this.nav = nav;

		if (this.preload && this.destination.allowPreload)
		{
			this.nav.QueueLoad(this.destination);
		}
	}

	public void Go()
	{
		this.nav.GoTo(this.destination);
	}
}

}
