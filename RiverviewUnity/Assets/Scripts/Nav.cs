using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace NotABear
{

public class Nav : MonoBehaviour
{
	List<MenuData> toLoad = new List<MenuData>();

	public void GoTo(MenuData def)
	{
		Load(def);
	}

	public void QueueLoad(MenuData def)
	{
		if (!this.toLoad.Contains(def))
		{
			this.toLoad.Add(def);
		}
	}

	public void Load(MenuData def)
	{
		StartCoroutine(LoadCoroutine(def));
	}

	IEnumerator LoadCoroutine(MenuData def)
	{
		AsyncOperation asyncLoad;
		if (def.type == MenuType.Solo)
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Single);
		}
		else
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Additive);
		}

		asyncLoad.allowSceneActivation = false;

		yield return asyncLoad;
	}
}

}
