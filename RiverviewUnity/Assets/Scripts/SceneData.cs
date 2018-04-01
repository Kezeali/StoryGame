using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cloverview
{

	public enum EnvCameraType
	{
		Perspective3D,
		Perspective2D,
		Orthographic
	}

	[CreateAssetMenu(fileName="Scene.asset", menuName="Cloverview/System/Scene Definition")]
	public class SceneData : ScriptableObject
	{
		public SceneField scene;
		public bool allowPreload = true;
		public bool background = false;
		public EnvCameraType cameraType;
	}

}