using UnityEngine;

namespace Cloverview
{

[CreateAssetMenu(fileName="NavDataIndex.asset", menuName="Cloverview/System/Nav Data Index")]
public class NavDataIndex : ScriptableObject
{
	public MenuData[] menus;
	public SceneData[] envScenes;
	public CommuteSceneData[] commutes;
}

}
