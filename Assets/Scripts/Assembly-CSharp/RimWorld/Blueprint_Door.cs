using Verse;

namespace RimWorld
{
	public class Blueprint_Door : Blueprint_Build
	{
		public override Graphic Graphic => base.DefaultGraphic;

		public override void Draw()
		{
			base.Rotation = Building_Door.DoorRotationAt(base.Position, base.Map);
			base.Draw();
		}
	}
}
