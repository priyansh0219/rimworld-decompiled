using UnityEngine;
using Verse;

namespace RimWorld
{
	public class TurretTop
	{
		private Building_Turret parentTurret;

		private float curRotationInt;

		private int ticksUntilIdleTurn;

		private int idleTurnTicksLeft;

		private bool idleTurnClockwise;

		private const float IdleTurnDegreesPerTick = 0.26f;

		private const int IdleTurnDuration = 140;

		private const int IdleTurnIntervalMin = 150;

		private const int IdleTurnIntervalMax = 350;

		public static readonly int ArtworkRotation = -90;

		public float CurRotation
		{
			get
			{
				return curRotationInt;
			}
			set
			{
				curRotationInt = value;
				if (curRotationInt > 360f)
				{
					curRotationInt -= 360f;
				}
				if (curRotationInt < 0f)
				{
					curRotationInt += 360f;
				}
			}
		}

		public void SetRotationFromOrientation()
		{
			CurRotation = parentTurret.Rotation.AsAngle;
		}

		public TurretTop(Building_Turret ParentTurret)
		{
			parentTurret = ParentTurret;
		}

		public void ForceFaceTarget(LocalTargetInfo targ)
		{
			if (targ.IsValid)
			{
				float curRotation = (targ.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
				CurRotation = curRotation;
			}
		}

		public void TurretTopTick()
		{
			LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
			if (currentTarget.IsValid)
			{
				float curRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
				CurRotation = curRotation;
				ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
			}
			else if (ticksUntilIdleTurn > 0)
			{
				ticksUntilIdleTurn--;
				if (ticksUntilIdleTurn == 0)
				{
					if (Rand.Value < 0.5f)
					{
						idleTurnClockwise = true;
					}
					else
					{
						idleTurnClockwise = false;
					}
					idleTurnTicksLeft = 140;
				}
			}
			else
			{
				if (idleTurnClockwise)
				{
					CurRotation += 0.26f;
				}
				else
				{
					CurRotation -= 0.26f;
				}
				idleTurnTicksLeft--;
				if (idleTurnTicksLeft <= 0)
				{
					ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
				}
			}
		}

		public void DrawTurret(Vector3 recoilDrawOffset, float recoilAngleOffset)
		{
			Vector3 v = new Vector3(parentTurret.def.building.turretTopOffset.x, 0f, parentTurret.def.building.turretTopOffset.y).RotatedBy(CurRotation);
			float turretTopDrawSize = parentTurret.def.building.turretTopDrawSize;
			v = v.RotatedBy(recoilAngleOffset);
			v += recoilDrawOffset;
			float num = parentTurret.CurrentEffectiveVerb?.AimAngleOverride ?? CurRotation;
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect + v, ((float)ArtworkRotation + num).ToQuat(), new Vector3(turretTopDrawSize, 1f, turretTopDrawSize));
			Graphics.DrawMesh(MeshPool.plane10, matrix, parentTurret.TurretTopMaterial, 0);
		}
	}
}
