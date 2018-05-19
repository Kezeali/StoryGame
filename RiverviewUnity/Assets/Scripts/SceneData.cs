using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cloverview
{

	public enum EnvCameraType
	{
		None,
		Perspective3D,
		Perspective2D,
		Orthographic
	}

	public enum SceneTransitionType
	{
		None,
		SceneController,
		Generic,
	}

	[CreateAssetMenu(fileName="Scene.asset", menuName="Cloverview/System/Scene Definition")]
	public class SceneData : ScriptableObject, IDataItem
	{
		public SceneField scene;
		public bool allowPreload = true;
		public bool background = false;
		public EnvCameraType cameraType = EnvCameraType.Orthographic;
		public SceneTransitionType transitionIn = SceneTransitionType.SceneController;
		public SceneTransitionType transitionOut = SceneTransitionType.SceneController;
		[Tooltip("Optional override for the transition animation. The default value is this scene definition's name.")]
		public string transitionInNameOverride;
		[Tooltip("Optional override for the transition animation. The default value is this scene definition's name.")]
		public string transitionOutNameOverride;
	}

}