using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld
{
	public class CompWakeUpDormant : ThingComp
	{
		public bool wakeUpIfTargetClose;

		private bool sentSignal;

		public int groupID = -1;

		private static List<Thing> tmpActivatedThings = new List<Thing>();

		private CompProperties_WakeUpDormant Props => (CompProperties_WakeUpDormant)props;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			wakeUpIfTargetClose = Props.wakeUpIfAnyTargetClose;
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent.IsHashIntervalTick(250))
			{
				TickRareWorker();
			}
		}

		public void TickRareWorker()
		{
			if (!parent.Spawned || parent.Faction == Faction.OfPlayer)
			{
				return;
			}
			if (wakeUpIfTargetClose)
			{
				int num = GenRadial.NumCellsInRadius(Props.wakeUpCheckRadius);
				for (int i = 0; i < num; i++)
				{
					IntVec3 intVec = parent.Position + GenRadial.RadialPattern[i];
					if (!intVec.InBounds(parent.Map) || !GenSight.LineOfSight(parent.Position, intVec, parent.Map))
					{
						continue;
					}
					foreach (Thing thing in intVec.GetThingList(parent.Map))
					{
						if (Props.wakeUpTargetingParams.CanTarget(thing))
						{
							Activate();
							return;
						}
					}
				}
			}
			if (Props.wakeUpOnThingConstructedRadius > 0f && GenClosest.ClosestThingReachable(parent.Position, parent.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(TraverseMode.NoPassClosedDoors), Props.wakeUpOnThingConstructedRadius, (Thing t) => (t.def.building == null || t.def.building.wakeDormantPawnsOnConstruction) && t.Faction == Faction.OfPlayer) != null)
			{
				Activate();
			}
		}

		public void Activate(bool sendSignal = true, bool silent = false)
		{
			if (sendSignal && !sentSignal)
			{
				if (!string.IsNullOrEmpty(Props.wakeUpSignalTag))
				{
					if (Props.onlyWakeUpSameFaction)
					{
						Find.SignalManager.SendSignal(new Signal(Props.wakeUpSignalTag, parent.Named("SUBJECT"), parent.Faction.Named("FACTION")));
					}
					else
					{
						Find.SignalManager.SendSignal(new Signal(Props.wakeUpSignalTag, parent.Named("SUBJECT")));
					}
				}
				if (!silent && parent.Spawned && Props.wakeUpSound != null)
				{
					Props.wakeUpSound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
				}
				if (!silent && (!Props.activateMessageKey.NullOrEmpty() || !Props.activatePluralMessageKey.NullOrEmpty()))
				{
					tmpActivatedThings.Clear();
					if (groupID >= 0)
					{
						List<Thing> list = parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.WakeUpDormant);
						for (int i = 0; i < list.Count; i++)
						{
							if (list[i].TryGetComp<CompWakeUpDormant>().groupID == groupID)
							{
								tmpActivatedThings.Add(list[i]);
							}
						}
					}
					else
					{
						tmpActivatedThings.Add(parent);
					}
					if (tmpActivatedThings.Count > 1 && !Props.activatePluralMessageKey.NullOrEmpty())
					{
						Messages.Message(Props.activatePluralMessageKey.Translate(), tmpActivatedThings, Props.activateMessageType ?? MessageTypeDefOf.NegativeEvent);
					}
					else if (tmpActivatedThings.Count > 0 && !Props.activateMessageKey.NullOrEmpty())
					{
						Messages.Message(Props.activateMessageKey.Translate(), tmpActivatedThings, Props.activateMessageType ?? MessageTypeDefOf.NegativeEvent);
					}
					tmpActivatedThings.Clear();
				}
				sentSignal = true;
			}
			CompCanBeDormant compCanBeDormant = parent.TryGetComp<CompCanBeDormant>();
			if (compCanBeDormant != null)
			{
				if (Props.wakeUpWithDelay)
				{
					compCanBeDormant.WakeUpWithDelay();
				}
				else
				{
					compCanBeDormant.WakeUp();
				}
			}
		}

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			if (Props.wakeUpOnDamage && totalDamageDealt > 0f && dinfo.Def.ExternalViolenceFor(parent))
			{
				Activate();
			}
		}

		public override void Notify_SignalReceived(Signal signal)
		{
			if (!string.IsNullOrEmpty(Props.wakeUpSignalTag))
			{
				sentSignal = true;
			}
		}

		public override string CompInspectStringExtra()
		{
			string text = base.CompInspectStringExtra();
			if (wakeUpIfTargetClose && !Props.radiusCheckInspectPaneKey.NullOrEmpty())
			{
				if (!text.NullOrEmpty())
				{
					text += "\n";
				}
				text = text + (string)(Props.radiusCheckInspectPaneKey.Translate() + ": ") + Mathf.RoundToInt(Props.wakeUpCheckRadius);
			}
			return text;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref sentSignal, "sentSignal", defaultValue: false);
			Scribe_Values.Look(ref groupID, "groupID", -1);
			Scribe_Values.Look(ref wakeUpIfTargetClose, "wakeUpIfColonistClose", defaultValue: false);
		}
	}
}
