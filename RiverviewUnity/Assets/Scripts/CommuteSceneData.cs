using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	// Any time an event happens between two activities, the plan executor checks to see if there's a commute scene for going from the previous activity's scene to the next activity's scene (sceneA->sceneB or vice versa). if there is a scene, it loads it up and runs the event there. otherwise the event happens in a generic blank scene
	[CreateAssetMenu(fileName="CommuteScene.asset", menuName="Cloverview/Commute Scene Definition")]
	public class CommuteSceneData : ScriptableObject, IDataItem
	{
		public SceneData commuteScene;
		[UnityEngine.Serialization.FormerlySerializedAs("fromScene")]
		public SceneData sceneA;
		[UnityEngine.Serialization.FormerlySerializedAs("toScene")]
		public SceneData sceneB;
	}

	public enum CommuteDirection
	{
		AtoB,
		BtoA
	}

}
