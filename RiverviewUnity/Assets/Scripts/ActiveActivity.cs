using System.Collections.Generic;
using UnityEngine;

namespace Cloverview
{

	public class ActiveActivity
	{
		public PlanActivityData def;
		public SubjectData subjectDef;
		public Nav.VisibleEnvScene envScene;
		public Cast cast;
		public int beginTimeUnit;
		public int timeUnitsSpent;

		List<CastEntity> castEntities = new List<CastEntity>();

		public void Progress()
		{
			Debug.Assert(this.def != null);
			if (this.castEntities.Count == 0) {
				this.castEntities.Clear();
				CastEntity.SelectCastMembers(this.castEntities, this.cast, this.def.leadRoles, this.def.extrasDescriptions, this.def.ToString());
				CastEntity.SpawnCast(this.castEntities, this.envScene.controller);
			}
			this.timeUnitsSpent += 1;
		}

		public void Finish()
		{
			for (int i = 0; i < this.castEntities.Count; ++i) {
				Object.Destroy(this.castEntities[i].characterBody.gameObject);
			}
			this.castEntities.Clear();

			StatBonusSource source = new StatBonusSource()
			{
				name = def.name,
				type = StatBonusSource.SourceType.Activity
			};
			this.cast.pc.AddStatBonuses(this.def.statBonuses, source, this.beginTimeUnit, this.timeUnitsSpent);
			this.cast.pc.AddStatBonuses(this.subjectDef.statBonuses, source, this.beginTimeUnit, this.timeUnitsSpent);
		}

		public void Pause()
		{
			for (int i = 0; i < this.castEntities.Count; ++i) {
				if (this.castEntities[i].characterBody != null) {
					this.castEntities[i].characterBody.gameObject.SetActive(false);
				}
			}
		}

		public void Resume()
		{
			for (int i = 0; i < this.castEntities.Count; ++i) {
				if (this.castEntities[i].characterBody != null) {
					this.castEntities[i].characterBody.gameObject.SetActive(true);
				}
			}
		}
	}

}
