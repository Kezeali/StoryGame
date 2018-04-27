using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Cloverview
{

public class AutoNavigate : MonoBehaviour, IServiceUser<Nav>, INavigator
{
	[Header("Menu Navigation")]
	public MenuData destination;
	[ReadOnly]
	public string parentScene;

	Nav nav;

	public void Start()
	{
		if (Application.isPlaying)
		{
			App.Register(this);
		}
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

	public void SetParentScene(string parentScene)
	{
		this.parentScene = parentScene;
	}
}

}
