using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Cloverview
{

// Useful on loading screens, to navigate to a new screen as soon as it loads.
public class AutoNavigate : MonoBehaviour, IServiceUser<Nav>, INavigator
{
	public bool immediate;

	[Header("Menu Navigation")]
	public MenuData destination;
	[ReadOnly]
	public string parentScene;

	Nav nav;

	public void OnEnable()
	{
		if (Application.isPlaying)
		{
			App.Register(this);
			if (this.immediate)
			{
				App.instance.Initialise();
			}
		}
	}

	public void OnDisable()
	{
		App.Deregister(this);
	}

	public void Initialise(Nav nav)
	{
		Debug.Assert(nav);
		this.nav = nav;

		if (this.destination != null)
		{
			this.nav.GoTo(this.destination, this.parentScene);
		}
	}

	public void CompleteInitialisation()
	{
	}

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}
}

}
