using UnityEngine;

namespace Cloverview
{

[CreateAssetMenu(fileName="SystemData.asset", menuName="Cloverview/System/System Data Index")]
public class SystemData : ScriptableObject
{
	public MenuData[] menus;
	public SceneData[] envScenes;
	public CommuteSceneData[] commutes;
	public DefaultSaveData[] defaultSaves;
}

}
