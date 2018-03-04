using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotABear
{

	[CreateAssetMenu(fileName="Scene.asset", menuName="Cloverview/System/Scene Definition")]
	public class SceneData : ScriptableObject
	{
		public SceneField scene;
		public bool allowPreload = true;
	}

}