using Verse;

namespace RimWorld
{
	public class CompProperties_Forbiddable : CompProperties
	{
		public bool allowNonPlayer;

		public CompProperties_Forbiddable()
		{
			compClass = typeof(CompForbiddable);
		}
	}
}
