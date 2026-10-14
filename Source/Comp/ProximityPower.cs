using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff.Comp;

internal class ProximityPower : ThingComp
{
    public CompProperties.ProximityPower Properties => (CompProperties.ProximityPower)props;
    public bool OverrideAutoPower;
    private readonly VacancyTimer vacancy = new();
    private Room lastRoom;
    private int nextCheck;
    private Thing trueParent;
    private Texture2D cachedCommandTex;
    public Thing TrueParent => trueParent ??= parent.GetFieldValue<Thing>("glower")
        ?? parent.GetType().BaseType.GetFieldValue<Thing>("glower", parent) ?? parent;
    private Texture2D CommandTex => cachedCommandTex ??= ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        trueParent = null;
        parent.Map.GetComponent<OccupancyMapComponent>().Register(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        map.GetComponent<OccupancyMapComponent>().Unregister(this);
        base.PostDeSpawn(map, mode);
        lastRoom = null;
        vacancy.Since = -1;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref OverrideAutoPower, nameof(OverrideAutoPower));
        Scribe_Values.Look(ref vacancy.Since, "vacancySince", -1);
    }

    public override void CompTick() { if (!AutoLight.Settings.NeedsFrequentChecks) CheckOccupancy(); }
    public override void CompTickRare() { if (!AutoLight.Settings.NeedsFrequentChecks) CheckOccupancy(); }

    public void ResetTimer(bool lightOn)
    {
        nextCheck = 0;
        vacancy.Since = -1;
    }

    public Room GetRoom() => TrueParent.GetRoom() ?? Compatability.MURWallLight.GetRoom(parent);

    internal void CheckOccupancy()
    {
        if (!parent.Spawned || OverrideAutoPower)
            return;
        var now = Find.TickManager.TicksGame;
        if (now < nextCheck)
            return;
        var flick = parent.TryGetComp<CompFlickable>();
        if (flick == null)
            return;
        var settings = AutoLight.Settings;
        ScheduleNext(now, flick.SwitchIsOn);
        var room = GetRoom();
        if (lastRoom != null && room != lastRoom)
        {
            vacancy.Since = -1;
        }
        lastRoom = room;
        if (room == null || room.PsychologicallyOutdoors)
        {
            vacancy.Since = -1;
            return;
        }
        var occupied = parent.Map.GetComponent<OccupancyMapComponent>().IsOccupied(room, Properties.offWhenSleeping);
        var mayTurnOff = vacancy.MayTurnOff(occupied, now, settings.VacancyDelayTicks);
        if (occupied && Properties.autoOn)
            SetAutomatic(flick, true);
        else if (!occupied && Properties.autoOff && mayTurnOff)
            SetAutomatic(flick, false);
        ScheduleNext(now, flick.SwitchIsOn);
    }

    private void ScheduleNext(int now, bool lightOn)
    {
        var settings = AutoLight.Settings;
        var rate = settings.OverrideCompProp
            ? (lightOn ? settings.OverrideOnTicks : settings.OverrideOffTicks)
            : Properties.checkRate;
        nextCheck = now + (settings.NeedsFrequentChecks ? OccupancyMapComponent.CheckInterval : System.Math.Max(1, rate));
    }
    private void SetAutomatic(CompFlickable flick, bool on)
    {
        if (flick.SwitchIsOn == on)
            return;
        AutomaticSwitchSounds.Mark(parent.TryGetComp<CompPowerTrader>(), on);
        flick.SwitchIsOn = on;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;
        if (Properties.showOverrideButton && parent.Faction == Faction.OfPlayer)
            yield return new Command_Toggle
            {
                icon = CommandTex,
                defaultLabel = "Override auto switch",
                defaultDesc = "Toggles whether the power is automatically on or off.",
                isActive = () => OverrideAutoPower,
                toggleAction = delegate
                {
                    OverrideAutoPower = !OverrideAutoPower;
                    AutomaticSwitchSounds.Cancel(parent.TryGetComp<CompPowerTrader>());
                    ResetTimer(false);
                    if (OverrideAutoPower)
                        parent.TryGetComp<CompFlickable>()?.ResetToOn();
                }
            };
    }
}
