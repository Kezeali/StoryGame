using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="OutfitItem.asset", menuName="Cloverview/Outfit Item Definition")]
	public class OutfitItemData : ScriptableObject, IDataItem
	{
		public GameObject prefab;
		public string targetPin;
	}

}