using UnityEngine;

namespace Cloverview
{

// Asset type to define a stat affector: things are applied to characters and modify their stats over time.
[CreateAssetMenu(fileName="StatAffector.asset", menuName="Cloverview/Stat Affector Definition")]
public class StatAffectorData : ScriptableObject, IDataItem
{
	public CharacterStatDefinition stat;
	// To determine the rate of change for a stat, the stat's current value is normalised to the min-max range of the stat. that value is then evaluated against this animation curve, and the result (i.e. the y-value of the curve at that point) is added to the total rate of change for the stat
	public AnimationCurve applicationCurve;
}

}
