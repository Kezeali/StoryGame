using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace NotABear
{

public class MenuNavigation : MonoBehaviour, IDataUser<Nav>
{
	public bool preload = true;
	public MenuData destination;

	Nav nav;

	public void Awake()
	{
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
