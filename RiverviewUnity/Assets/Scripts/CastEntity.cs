using System.Collections.Generic;
using UnityEngine;

namespace Cloverview
{

// Cast character with information about their position & representation in the current scene. Can be serialised to save files.
[System.Serializable]
public struct CastEntity
{
	public Character castMember;
	public StageMarkData mark;
	[System.NonSerialized]
	public CharacterBody characterBody;
	
	public static void SelectCastMembers(List<CastEntity> activeCast, Cast liveCast, SceneRole[] leadRoles, CastingCharacterDescription[] extrasDescriptions, string debugContext)
	{
		// This index is used to make it efficient to determine which cast members are still availble for selection (every time a cast member is selected the index is moved forward and the selected member is moved past the new end point)
		int liveCastAvailableListEnd = liveCast.leadNpcs.Count;

		for (int roleIndex = 0; roleIndex < leadRoles.Length; ++roleIndex) {
			SceneRole leadRole = leadRoles[roleIndex];
			Character castMember = null;
			if (leadRole.role == liveCast.pc.role) {
				castMember = liveCast.pc;
			} else {
				for (int castMemberIndex = 0; castMemberIndex < liveCastAvailableListEnd; ++castMemberIndex) {
					if (liveCast.leadNpcs[castMemberIndex].role == leadRole.role) {
						castMember = liveCast.leadNpcs[castMemberIndex];
						// Move the selected cast member to the end of the cast list to indicate that they are now unavailable for selection
						liveCastAvailableListEnd--;
						liveCast.leadNpcs[castMemberIndex] = liveCast.leadNpcs[liveCastAvailableListEnd];
						liveCast.leadNpcs[liveCastAvailableListEnd] = castMember;
						break;
					}
				}
			}
			if (castMember != null) {
				CharacterBody bodyInstance = Object.Instantiate(castMember.bodyPrefab);
				bodyInstance.Dress(castMember.outfitItems);
				
				CastEntity castEntity = new CastEntity()
				{
					castMember = castMember,
					mark = leadRole.mark,
					characterBody = bodyInstance
				};
				activeCast.Add(castEntity);

				Debug.LogFormat("'{0}' cast in role '{1}' at index {2} in {3}", castMember, leadRole, roleIndex, debugContext);
			} else {
				Debug.LogWarningFormat("No cast member available for the '{0}' role at index {1} in {2}", leadRole.role, roleIndex, debugContext);
			}
		}

		if (liveCastAvailableListEnd <= 0) {
			Debug.LogFormat("No cast members available to fill extra casting in {0}.", debugContext);
			return;
		}

		for (int extraIndex = 0; extraIndex < extrasDescriptions.Length; ++extraIndex) {
			CastingCharacterDescription extraDescription = extrasDescriptions[extraIndex];

			int validCastMembersEnd = extraDescription.SortActors(liveCast.leadNpcs, liveCastAvailableListEnd);

			Character castMember = null;
			if (validCastMembersEnd > 0) {
				castMember = liveCast.leadNpcs[0];
				// Move the selected cast member to the end of the cast list to indicate that they are now unavailable for selection
				liveCastAvailableListEnd--;
				liveCast.leadNpcs[0] = liveCast.leadNpcs[liveCastAvailableListEnd];
				liveCast.leadNpcs[liveCastAvailableListEnd] = castMember;
			}
			if (castMember != null) {
				CharacterBody bodyInstance = Object.Instantiate(castMember.bodyPrefab);
				bodyInstance.Dress(castMember.outfitItems);

				CastEntity castEntity = new CastEntity()
				{
					castMember = castMember,
					mark = extraDescription.mark,
					characterBody = bodyInstance
				};
				activeCast.Add(castEntity);

				Debug.LogFormat("'{0}' cast as extra '{1}' at index {2} in {3}", castMember, extraDescription, extraIndex, debugContext);
			} else {
				Debug.LogFormat("No cast member available for extra '{0}' at index {1} in {2}", extraDescription, extraIndex, debugContext);
			}
			if (liveCastAvailableListEnd <= 0) {
				Debug.LogFormat("No more cast members available to fill extra casting in {0}", debugContext);
				break;
			}
		}
	}

	public static void SpawnCast(List<CastEntity> activeCast, EnvSceneController controller)
	{
	}
}

}
