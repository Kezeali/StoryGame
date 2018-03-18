using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

[CreateAssetMenu(fileName="Role.asset", menuName="Cloverview/Role Definition")]
public class RoleData : ScriptableObject, IDataItem
{
	public Character charSheet;
	// Potential fields: Name gen, favourites gen, tags gen
}

}
