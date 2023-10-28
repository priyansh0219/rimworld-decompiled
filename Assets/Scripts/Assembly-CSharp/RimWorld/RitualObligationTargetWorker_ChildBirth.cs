using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public class RitualObligationTargetWorker_ChildBirth : RitualObligationTargetFilter
	{
		public RitualObligationTargetWorker_ChildBirth()
		{
		}

		public RitualObligationTargetWorker_ChildBirth(RitualObligationTargetFilterDef def)
			: base(def)
		{
		}

		public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
		{
			foreach (Pawn item in ColonistsInLabor(map))
			{
				foreach (Building_Bed item2 in PregnancyUtility.BedsForBirth(item))
				{
					yield return item2;
				}
			}
		}

		protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
		{
			if (!target.HasThing)
			{
				return false;
			}
			if (!(target.Thing is Building_Bed bed))
			{
				return false;
			}
			if (def.colonistThingsOnly && (target.Thing.Faction == null || !target.Thing.Faction.IsPlayer))
			{
				return false;
			}
			foreach (Pawn item in ColonistsInLabor(target.Map))
			{
				if (PregnancyUtility.IsUsableBedFor(item, item, bed))
				{
					return true;
				}
			}
			return false;
		}

		protected IEnumerable<Pawn> ColonistsInLabor(Map map)
		{
			foreach (Pawn freeColonistsAndPrisoner in map.mapPawns.FreeColonistsAndPrisoners)
			{
				if (freeColonistsAndPrisoner.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor))
				{
					yield return freeColonistsAndPrisoner;
				}
			}
		}

		public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
		{
			yield return "RitualTargetChildBirth".Translate();
		}
	}
}
