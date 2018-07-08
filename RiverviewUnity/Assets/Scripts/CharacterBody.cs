using UnityEngine;

namespace Cloverview
{

public class CharacterBody : MonoBehaviour
{
	[System.Serializable]
	public struct DressupPin
	{
		public string name;
		[System.NonSerialized]
		public Transform bone;
		[System.NonSerialized]
		public Transform wornItem;
		[System.NonSerialized]
		public Transform heldItem;
	}

	public DressupPin[] pins;

	public void Undress()
	{
		for (int i = 0; i < this.pins.Length; ++i) {
			Object.Destroy(this.pins[i].wornItem);
		}
	}

	public void Dress(OutfitItemData[] outfit)
	{
		this.Undress();
		for (int outfitItemIndex = 0; outfitItemIndex < outfit.Length; ++outfitItemIndex) {
			this.Wear(outfit[outfitItemIndex]);
		}
	}

	public void Wear(OutfitItemData item)
	{
		for (int pinIndex = 0; pinIndex < this.pins.Length; ++pinIndex) {
			if (this.pins[pinIndex].name == item.targetPin) {
				DressupPin pin = this.pins[pinIndex];
				GameObject instance = Object.Instantiate(item.prefab, pin.bone);
				pin.wornItem = instance.transform;
				this.pins[pinIndex] = pin;
				break;
			}
		}
	}

	public void Hold(CharacterProp[] props)
	{
		for (int propIndex = 0; propIndex < props.Length; ++propIndex) {
			CharacterProp prop = props[propIndex];
			this.Hold(prop);
		}
	}

	public void Hold(CharacterProp prop)
	{
		for (int pinIndex = 0; pinIndex < this.pins.Length; ++pinIndex) {
			if (this.pins[pinIndex].name == prop.targetPin) {
				DressupPin pin = this.pins[pinIndex];
				GameObject instance = Object.Instantiate(prop.prefab, pin.bone);
				pin.heldItem = instance.transform;
				this.pins[pinIndex] = pin;
				break;
			}
		}
	}
}

}
