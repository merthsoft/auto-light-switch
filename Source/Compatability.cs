using Verse;

namespace Merthsoft.AutoOnAutoOff
{
    internal static class Compatability
    {
        public static class MURWallLight
        {
            public static Room GetRoom(ThingWithComps parent)
                => RegionAndRoomQuery.RoomAtFast(parent.Position + IntVec3.North.RotatedBy(parent.Rotation), parent.Map);
        }
    }
}
