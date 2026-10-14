using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Merthsoft.AutoOnAutoOff;

// One pawn traversal per map snapshot, shared by every lamp. Never scans map things.
public sealed class OccupancyMapComponent : MapComponent
{
    public const int CheckInterval = 30;
    private readonly HashSet<Comp.ProximityPower> lights = new();
    private readonly Dictionary<Room, int> occupants = new();
    private readonly Dictionary<Room, int> doorwayOccupants = new();
    private int snapshotTick = -1;

    public OccupancyMapComponent(Map map) : base(map) { }

    internal void Register(Comp.ProximityPower light) => lights.Add(light);
    internal void Unregister(Comp.ProximityPower light) => lights.Remove(light);

    public override void MapComponentTick()
    {
        if (!AutoLight.Settings.NeedsFrequentChecks || Find.TickManager.TicksGame % CheckInterval != 0)
            return;
        foreach (var light in lights)
            light.CheckOccupancy();
    }

    internal bool IsOccupied(Room room, bool offWhenSleeping)
    {
        Refresh();
        var mask = offWhenSleeping ? 1 : 3;
        if (!AutoLight.Settings.IgnoreAnimals)
            mask |= offWhenSleeping ? 4 : 12;
        occupants.TryGetValue(room, out var inside);
        doorwayOccupants.TryGetValue(room, out var doorway);
        return ((inside | (AutoLight.Settings.FastEntryDetection ? doorway : 0)) & mask) != 0;
    }

    private void Refresh()
    {
        var now = Find.TickManager.TicksGame;
        if (snapshotTick >= 0 && now >= snapshotTick && now - snapshotTick < CheckInterval)
            return;
        snapshotTick = now;
        occupants.Clear();
        doorwayOccupants.Clear();
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            var bit = (pawn.RaceProps.Animal ? 4 : 1) * (pawn.Awake() ? 1 : 2);
            Add(occupants, pawn.GetRoom(), bit);
            // Occupancy, not door events: held-open doors work and unrelated doors do nothing.
            if (pawn.Position.GetEdifice(map) is not Building_Door)
                continue;
            foreach (var direction in GenAdj.CardinalDirections)
            {
                var cell = pawn.Position + direction;
                if (cell.InBounds(map))
                    Add(doorwayOccupants, RegionAndRoomQuery.RoomAt(cell, map), bit);
            }
        }
    }

    private static void Add(Dictionary<Room, int> rooms, Room room, int bit)
    {
        if (room == null)
            return;
        rooms.TryGetValue(room, out var previous);
        rooms[room] = previous | bit;
    }
}
