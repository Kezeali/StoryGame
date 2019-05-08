using UnityEngine;

namespace Cloverview
{

	// Asset type which defines how an outfit asset should be worn on a character.
	[CreateAssetMenu(fileName="OutfitItem.asset", menuName="Cloverview/Outfit Item Definition")]
	public class OutfitItemData : ScriptableObject, IDataItem
	{
		public GameObject prefab;
		public string targetPin;
	}

}