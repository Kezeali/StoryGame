using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cloverview
{

	[CreateAssetMenu(fileName="Scene.asset", menuName="Cloverview/System/Scene Definition")]
	public class SceneData : ScriptableObject
	{
		public SceneField scene;
		public bool allowPreload = true;
		public bool background = false;
	}

}