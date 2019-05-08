using UnityEngine;

namespace Cloverview
{

// Asset type which is just an index of other asset types. See also DataIndex.
[CreateAssetMenu(fileName="NavDataIndex.asset", menuName="Cloverview/System/Nav Data Index")]
public class NavDataIndex : ScriptableObject
{
	public MenuData[] menus;
	public SceneData[] envScenes;
	public CommuteSceneData[] commutes;
}

}
