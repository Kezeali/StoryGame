using UnityEngine;

namespace Cloverview
{

[CreateAssetMenu(fileName="Data.asset", menuName="Cloverview/Data Index")]
public class GameData : ScriptableObject
{
	public RoleData[] roles;
}

}
