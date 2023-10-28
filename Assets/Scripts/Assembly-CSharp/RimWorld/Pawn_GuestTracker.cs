using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimWorld
{
	public class Pawn_GuestTracker : IExposable
	{
		private Pawn pawn;

		public PrisonerInteractionModeDef interactionMode = PrisonerInteractionModeDefOf.NoInteraction;

		public SlaveInteractionModeDef slaveInteractionMode = SlaveInteractionModeDefOf.NoInteraction;

		private Faction hostFactionInt;

		public GuestStatus guestStatusInt;

		public JoinStatus joinStatus;

		private Faction slaveFactionInt;

		public string lastRecruiterName;

		public int lastRecruiterOpinion;

		public bool hasOpinionOfLastRecruiter;

		public ResistanceInteractionData lastResistanceInteractionData;

		public ResistanceInteractionData finalResistanceInteractionData;

		private bool releasedInt;

		private int ticksWhenAllowedToEscapeAgain;

		public IntVec3 spotToWaitInsteadOfEscaping = IntVec3.Invalid;

		public int lastPrisonBreakTicks = -1;

		public bool everParticipatedInPrisonBreak;

		public float resistance = -1f;

		public float will = -1f;

		public Ideo ideoForConversion;

		private bool recruitable = true;

		private bool everEnslaved;

		public bool getRescuedThoughtOnUndownedBecauseOfPlayer;

		private const int DefaultWaitInsteadOfEscapingTicks = 25000;

		public const int MinInteractionInterval = 10000;

		public const int MaxInteractionsPerDay = 2;

		private const int CheckInitiatePrisonBreakIntervalTicks = 2500;

		private const int CheckInitiateSlaveRebellionIntervalTicks = 2500;

		private const int MinSlaveSuppressionIntervalTicks = 60000;

		public const float DefaultWillIfPreviouslyEnslaved = 2.5f;

		private const int CheckHemogenBillInterval = 15000;

		public Faction HostFaction => hostFactionInt;

		public Faction SlaveFaction => slaveFactionInt;

		public GuestStatus GuestStatus => guestStatusInt;

		public bool CanBeBroughtFood
		{
			get
			{
				if (interactionMode != PrisonerInteractionModeDefOf.Execution)
				{
					if (interactionMode == PrisonerInteractionModeDefOf.Release)
					{
						return pawn.Downed;
					}
					return true;
				}
				return false;
			}
		}

		public bool IsPrisoner => guestStatusInt == GuestStatus.Prisoner;

		public bool IsSlave => guestStatusInt == GuestStatus.Slave;

		public bool ScheduledForInteraction
		{
			get
			{
				if (pawn.mindState.lastAssignedInteractTime < Find.TickManager.TicksGame - 10000)
				{
					return pawn.mindState.interactionsToday < 2;
				}
				return false;
			}
		}

		public bool Released
		{
			get
			{
				return releasedInt;
			}
			set
			{
				if (value != releasedInt)
				{
					releasedInt = value;
					ReachabilityUtility.ClearCacheFor(pawn);
				}
			}
		}

		public bool PrisonerIsSecure
		{
			get
			{
				if (Released)
				{
					return false;
				}
				if (pawn.HostFaction == null)
				{
					return false;
				}
				if (pawn.InMentalState)
				{
					return false;
				}
				if (pawn.Spawned)
				{
					if (pawn.jobs.curJob != null && pawn.jobs.curJob.exitMapOnArrival)
					{
						return false;
					}
					if (PrisonBreakUtility.IsPrisonBreaking(pawn))
					{
						return false;
					}
				}
				return true;
			}
		}

		public bool SlaveIsSecure
		{
			get
			{
				if (Released)
				{
					return false;
				}
				if (pawn.InMentalState)
				{
					return false;
				}
				if (pawn.Spawned)
				{
					if (pawn.jobs.curJob != null && pawn.jobs.curJob.exitMapOnArrival)
					{
						return false;
					}
					if (SlaveRebellionUtility.IsRebelling(pawn))
					{
						return false;
					}
				}
				return true;
			}
		}

		public bool ShouldWaitInsteadOfEscaping
		{
			get
			{
				if (!IsPrisoner)
				{
					return false;
				}
				Map mapHeld = pawn.MapHeld;
				if (mapHeld == null)
				{
					return false;
				}
				if (mapHeld.mapPawns.FreeColonistsSpawnedCount == 0)
				{
					return false;
				}
				return Find.TickManager.TicksGame < ticksWhenAllowedToEscapeAgain;
			}
		}

		public float Resistance => resistance;

		public bool Recruitable
		{
			get
			{
				if (!Find.Storyteller.difficulty.unwaveringPrisoners)
				{
					return true;
				}
				if (PawnUtility.EverBeenColonistOrTameAnimal(pawn))
				{
					return true;
				}
				if (pawn.Faction != null && pawn.Faction == Faction.OfPlayerSilentFail && !pawn.IsSlave && !pawn.IsPrisoner)
				{
					return true;
				}
				if (pawn.kindDef == PawnKindDefOf.WildMan)
				{
					return true;
				}
				Lord lord = pawn.GetLord();
				if (lord != null && lord.LordJob is LordJob_TradeWithColony && pawn.GetTraderCaravanRole() == TraderCaravanRole.Chattel)
				{
					return true;
				}
				return recruitable;
			}
			set
			{
				recruitable = value;
			}
		}

		public bool ScheduledForSlaveSuppression => pawn.mindState.lastSlaveSuppressedTick < Find.TickManager.TicksGame - 60000;

		public bool EverEnslaved => everEnslaved;

		public Pawn_GuestTracker()
		{
		}

		public Pawn_GuestTracker(Pawn pawn)
		{
			this.pawn = pawn;
		}

		public void SetupRecruitable()
		{
			recruitable = !Rand.Chance(HealthTuning.NonRecruitableChanceOverPopulationIntentCurve.Evaluate(StorytellerUtilityPopulation.PopulationIntent));
		}

		public void GuestTrackerTick()
		{
			if (ModsConfig.BiotechActive && pawn.Spawned && pawn.IsHashIntervalTick(15000) && guestStatusInt == GuestStatus.Prisoner && interactionMode == PrisonerInteractionModeDefOf.HemogenFarm && pawn.BillStack != null && !pawn.BillStack.Bills.Any((Bill x) => x.recipe == RecipeDefOf.ExtractHemogenPack) && RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn) && !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss))
			{
				HealthCardUtility.CreateSurgeryBill(pawn, RecipeDefOf.ExtractHemogenPack, null, null, sendMessages: false);
			}
			if (pawn.IsHashIntervalTick(2500))
			{
				float num = PrisonBreakUtility.InitiatePrisonBreakMtbDays(pawn);
				if (num >= 0f && Rand.MTBEventOccurs(num, 60000f, 2500f))
				{
					PrisonBreakUtility.StartPrisonBreak(pawn);
				}
			}
			if (pawn.IsHashIntervalTick(2500))
			{
				float num2 = SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn);
				if (num2 >= 0f && Rand.MTBEventOccurs(num2, 60000f, 2500f))
				{
					SlaveRebellionUtility.StartSlaveRebellion(pawn);
				}
			}
		}

		public void RandomizeJoinStatus()
		{
			if (ModsConfig.IdeologyActive)
			{
				joinStatus = ((!(Rand.Value < 0.75f)) ? JoinStatus.JoinAsColonist : JoinStatus.JoinAsSlave);
			}
			else
			{
				joinStatus = JoinStatus.JoinAsColonist;
			}
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref hostFactionInt, "hostFaction");
			Scribe_References.Look(ref slaveFactionInt, "slaveFaction");
			Scribe_Values.Look(ref guestStatusInt, "guestStatus", GuestStatus.Guest);
			Scribe_Values.Look(ref joinStatus, "joinStatus", JoinStatus.Undefined);
			Scribe_Defs.Look(ref interactionMode, "interactionMode");
			Scribe_Defs.Look(ref slaveInteractionMode, "slaveInteractionMode");
			Scribe_Values.Look(ref releasedInt, "released", defaultValue: false);
			Scribe_Values.Look(ref ticksWhenAllowedToEscapeAgain, "ticksWhenAllowedToEscapeAgain", 0);
			Scribe_Values.Look(ref spotToWaitInsteadOfEscaping, "spotToWaitInsteadOfEscaping");
			Scribe_Values.Look(ref lastPrisonBreakTicks, "lastPrisonBreakTicks", 0);
			Scribe_Values.Look(ref everParticipatedInPrisonBreak, "everParticipatedInPrisonBreak", defaultValue: false);
			Scribe_Values.Look(ref getRescuedThoughtOnUndownedBecauseOfPlayer, "getRescuedThoughtOnUndownedBecauseOfPlayer", defaultValue: false);
			Scribe_Values.Look(ref resistance, "resistance", -1f);
			Scribe_Values.Look(ref will, "will", -1f);
			Scribe_Values.Look(ref lastRecruiterOpinion, "lastRecruiterOpinion", 0);
			Scribe_Values.Look(ref lastRecruiterName, "lastRecruiterName");
			Scribe_Values.Look(ref hasOpinionOfLastRecruiter, "hasOpinionOfLastRecruiter", defaultValue: false);
			Scribe_Values.Look(ref everEnslaved, "everEnslaved", defaultValue: false);
			Scribe_References.Look(ref ideoForConversion, "ideoForConversion");
			Scribe_Values.Look(ref recruitable, "recruitable", defaultValue: true);
			Scribe_Deep.Look(ref lastResistanceInteractionData, "lastResistanceInteractionData");
			Scribe_Deep.Look(ref finalResistanceInteractionData, "finalResistanceInteractionData");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (!interactionMode.allowOnWildMan && pawn.IsWildMan())
				{
					interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
				}
				if (joinStatus == JoinStatus.Undefined)
				{
					RandomizeJoinStatus();
				}
			}
			BackCompatibility.PostExposeData(this);
		}

		public void SetRecruitmentData(Pawn recruiter)
		{
			lastRecruiterName = recruiter.LabelShort;
			hasOpinionOfLastRecruiter = pawn.relations != null;
			lastRecruiterOpinion = (hasOpinionOfLastRecruiter ? pawn.relations.OpinionOf(recruiter) : 0);
			finalResistanceInteractionData = lastResistanceInteractionData;
			lastResistanceInteractionData = null;
		}

		public void SetLastResistanceReduceData(Pawn initiator, float resistanceReduce, float negotiationFactor, float moodFactor, float opinionFactor)
		{
			lastResistanceInteractionData = new ResistanceInteractionData
			{
				initiatorNegotiationAbilityFactor = negotiationFactor,
				recruiteeMoodFactor = moodFactor,
				resistanceReduction = resistanceReduce,
				initiatorName = initiator.LabelShort,
				recruiterOpinionFactor = opinionFactor
			};
		}

		public void Notify_WardensOfIdeoLost(Ideo ideo)
		{
			if (ModsConfig.IdeologyActive && interactionMode == PrisonerInteractionModeDefOf.Convert && ideoForConversion != null && ideoForConversion == ideo)
			{
				interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
				ideoForConversion = null;
				Messages.Message("MessageNoWardenOfIdeo".Translate(pawn.Named("PRISONER"), ideo.memberName.Named("MEMBERNAME")), new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, historical: false);
			}
		}

		public void SetGuestStatus(Faction newHost, GuestStatus guestStatus = GuestStatus.Guest)
		{
			if (newHost != null)
			{
				Released = false;
			}
			switch (guestStatus)
			{
			case GuestStatus.Prisoner:
			{
				if (newHost == HostFaction && IsPrisoner)
				{
					return;
				}
				pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
				pawn.ClearMind(newHost != null);
				pawn.DropAndForbidEverything();
				pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.MadePrisoner);
				if (pawn.Drafted)
				{
					pawn.drafter.Drafted = false;
				}
				float num = pawn.kindDef.initialResistanceRange.Value.RandomInRange;
				if (pawn.royalty != null)
				{
					RoyalTitle mostSeniorTitle = pawn.royalty.MostSeniorTitle;
					if (mostSeniorTitle != null)
					{
						num += mostSeniorTitle.def.recruitmentResistanceOffset;
					}
				}
				resistance = GenMath.RoundRandom(num);
				will = (everEnslaved ? 2.5f : pawn.kindDef.initialWillRange.Value.RandomInRange);
				if (ModsConfig.IdeologyActive && interactionMode == PrisonerInteractionModeDefOf.Enslave)
				{
					interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
				}
				if (guestStatusInt == GuestStatus.Slave && slaveFactionInt != null)
				{
					pawn.SetFaction(slaveFactionInt);
				}
				break;
			}
			case GuestStatus.Slave:
			{
				if (newHost == pawn.Faction && IsSlave)
				{
					return;
				}
				pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.MadeSlave);
				Faction faction = pawn.Faction;
				if (pawn.Faction != newHost)
				{
					pawn.SetFaction(newHost);
				}
				if (slaveFactionInt == null)
				{
					slaveFactionInt = faction;
				}
				if (!everEnslaved || slaveInteractionMode == SlaveInteractionModeDefOf.Imprison || slaveInteractionMode == SlaveInteractionModeDefOf.Emancipate)
				{
					slaveInteractionMode = SlaveInteractionModeDefOf.Suppress;
				}
				everEnslaved = true;
				break;
			}
			case GuestStatus.Guest:
				if (pawn.Faction.HostileTo(newHost))
				{
					Log.Error(string.Concat("Tried to make ", pawn, " a guest of ", newHost, " but their faction ", pawn.Faction, " is hostile to ", newHost));
					return;
				}
				if (newHost != null && newHost == pawn.Faction)
				{
					Log.Error(string.Concat("Tried to make ", pawn, " a guest of their own faction ", pawn.Faction));
					return;
				}
				if (newHost == null)
				{
					slaveFactionInt = null;
				}
				break;
			default:
				Log.Error($"Unknown GuestStatus type {guestStatus}");
				return;
			}
			guestStatusInt = guestStatus;
			Faction faction2 = hostFactionInt;
			if (guestStatus != GuestStatus.Slave)
			{
				hostFactionInt = newHost;
			}
			else
			{
				hostFactionInt = null;
			}
			pawn.Notify_DisabledWorkTypesChanged();
			PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
			pawn.health.surgeryBills.Clear();
			if (pawn.ownership != null)
			{
				pawn.ownership.Notify_ChangedGuestStatus();
			}
			pawn.Ideo?.Notify_MemberGuestStatusChanged(pawn);
			if (ModsConfig.BiotechActive && pawn.mechanitor != null)
			{
				pawn.mechanitor.Notify_ChangedGuestStatus();
			}
			ReachabilityUtility.ClearCacheFor(pawn);
			if (pawn.Spawned)
			{
				pawn.Map.mapPawns.UpdateRegistryForPawn(pawn);
				pawn.Map.attackTargetsCache.UpdateTarget(pawn);
			}
			AddictionUtility.CheckDrugAddictionTeachOpportunity(pawn);
			if ((guestStatus == GuestStatus.Prisoner || guestStatus == GuestStatus.Slave) && pawn.playerSettings != null)
			{
				pawn.playerSettings.ResetMedicalCare();
			}
			if (faction2 != hostFactionInt)
			{
				QuestUtility.SendQuestTargetSignals(pawn.questTags, "ChangedHostFaction", pawn.Named("SUBJECT"), hostFactionInt.Named("FACTION"));
			}
		}

		public void CapturedBy(Faction by, Pawn byPawn = null)
		{
			pawn.HomeFaction?.Notify_MemberCaptured(pawn, by);
			SetGuestStatus(by, GuestStatus.Prisoner);
			if (IsPrisoner && byPawn != null)
			{
				TaleRecorder.RecordTale(TaleDefOf.Captured, byPawn, pawn);
				byPawn.records.Increment(RecordDefOf.PeopleCaptured);
			}
			if (by == Faction.OfPlayer && !Recruitable && !Find.History.everCapturedUnrecruitablePawn)
			{
				Find.History.everCapturedUnrecruitablePawn = true;
				TaggedString taggedString = (ModsConfig.IdeologyActive ? ("OptionalEnslaveDesc".Translate(pawn.Named("PAWN")) + ", ") : TaggedString.Empty);
				Find.LetterStack.ReceiveLetter("LetterLabelUnrecruitablePawnCaptured".Translate(), "LetterTextUnrecruitablePawnCaptured".Translate(pawn.Named("PAWN"), taggedString.Named("OPTIONALENSLAVEDESC")), LetterDefOf.NeutralEvent, pawn);
			}
		}

		public void WaitInsteadOfEscapingForDefaultTicks()
		{
			WaitInsteadOfEscapingFor(25000);
		}

		public void WaitInsteadOfEscapingFor(int ticks)
		{
			if (IsPrisoner)
			{
				ticksWhenAllowedToEscapeAgain = Find.TickManager.TicksGame + ticks;
				spotToWaitInsteadOfEscaping = IntVec3.Invalid;
			}
		}

		public Texture2D GetIcon()
		{
			return GuestUtility.GetGuestIcon(guestStatusInt);
		}

		public string GetLabel()
		{
			if (IsSlave)
			{
				return "Slave".Translate();
			}
			return null;
		}

		internal void Notify_PawnUndowned()
		{
			if (pawn.RaceProps.Humanlike && (HostFaction.IsPlayerSafe() || (pawn.IsWildMan() && pawn.InBed() && pawn.CurrentBed().Faction == Faction.OfPlayer)) && !IsPrisoner && pawn.SpawnedOrAnyParentSpawned)
			{
				if (getRescuedThoughtOnUndownedBecauseOfPlayer && pawn.needs != null && pawn.needs.mood != null)
				{
					pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.Rescued);
				}
				if (pawn.Faction == null || pawn.Faction.def.rescueesCanJoin)
				{
					Map mapHeld = pawn.MapHeld;
					float num = ((pawn.SafeTemperatureRange().Includes(mapHeld.mapTemperature.OutdoorTemp) && !mapHeld.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) && (!ModsConfig.BiotechActive || !mapHeld.gameConditionManager.ConditionIsActive(GameConditionDefOf.NoxiousHaze))) ? 0.5f : 1f);
					if (Rand.ValueSeeded(pawn.thingIDNumber ^ 0x88F8E4) < num)
					{
						pawn.SetFaction(Faction.OfPlayer);
						Find.LetterStack.ReceiveLetter("LetterLabelRescueeJoins".Translate(pawn.Named("PAWN")), "LetterRescueeJoins".Translate(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, pawn);
					}
					else
					{
						Messages.Message("MessageRescueeDidntJoin".Translate().AdjustedFor(pawn), pawn, MessageTypeDefOf.NeutralEvent);
					}
				}
			}
			getRescuedThoughtOnUndownedBecauseOfPlayer = false;
		}

		public void Notify_PawnRecruited()
		{
			slaveFactionInt = null;
		}
	}
}
