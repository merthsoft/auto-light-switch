using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Merthsoft.AutoOnAutoOff;

internal enum DeviceKind { BillTable, ResearchBench, MedicalMonitor }

internal sealed class DeviceAutomationProperties : Verse.CompProperties
{
    public DeviceKind kind;
    public DeviceAutomationProperties() => compClass = typeof(DeviceAutomation);
}

// Explicit vanilla allowlist: autonomous production, temperature, defensive and
// food-dispensing devices need different rules and must not opt in by inheritance.
[StaticConstructorOnStartup]
internal static class SupportedDevices
{
    internal static readonly string[] BillTables =
    {
        "ElectricTailoringBench", "ElectricSmithy", "TableMachining", "ElectricStove",
        "ElectricSmelter", "BiofuelRefinery", "FabricationBench", "ElectricCrematorium"
    };

    static SupportedDevices()
    {
        foreach (var name in BillTables)
            Add(name, DeviceKind.BillTable);
        Add("HiTechResearchBench", DeviceKind.ResearchBench);
        Add("VitalsMonitor", DeviceKind.MedicalMonitor);
    }

    private static void Add(string name, DeviceKind kind)
    {
        var def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
        if (def == null || def.GetCompProperties<DeviceAutomationProperties>() != null)
            return;
        // Skip replacement power/thing classes, generators, and mixed light automation.
        var power = def.GetCompProperties<CompProperties_Power>();
        var expectedClass = kind == DeviceKind.BillTable
            ? (def.thingClass == typeof(Building_WorkTable) || def.thingClass == typeof(Building_WorkTable_HeatPush))
            : kind == DeviceKind.ResearchBench ? def.thingClass == typeof(Building_ResearchBench)
            : def.thingClass == typeof(Building);
        if (!expectedClass || power?.compClass != typeof(CompPowerTrader) || power.PowerConsumption <= 1
            || def.GetCompProperties<CompProperties_Flickable>() == null
            || def.GetCompProperties<CompProperties.ProximityPower>() != null
            || (kind == DeviceKind.MedicalMonitor && def.GetCompProperties<CompProperties_Facility>()?.compClass != typeof(CompFacility)))
        {
            Log.Warning($"Auto Light Switch skipped power saving for changed or unsupported device {name}.");
            return;
        }
        def.comps.Add(new DeviceAutomationProperties { kind = kind });
    }
}

internal sealed class DeviceAutomation : ThingComp
{
    private const int IntentGraceTicks = 60;
    private bool disabled;
    private int intendedUntil = -1;
    private Texture2D cachedIcon;
    internal DeviceKind Kind => ((DeviceAutomationProperties)props).kind;
    internal bool Enabled => !disabled && parent.Spawned && parent.Faction == Faction.OfPlayer
        && (Kind == DeviceKind.MedicalMonitor ? AutoLight.Settings.AutomateMedicalMonitors : AutoLight.Settings.AutomateWorkbenches);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref disabled, "disableDevicePowerSaving", false);
    }

    internal void NotifyWorkIntent(Job job)
    {
        if (Enabled && job?.targetA.Thing == parent && MatchesJob(job.def))
            intendedUntil = Find.TickManager.TicksGame + IntentGraceTicks;
    }

    internal bool MatchesJob(JobDef job) => Kind == DeviceKind.BillTable ? job == JobDefOf.DoBill
        : Kind == DeviceKind.ResearchBench && job == JobDefOf.Research;

    internal bool IsInUse()
    {
        if (Kind == DeviceKind.MedicalMonitor)
        {
            // Raw links do not depend on the monitor currently providing a bonus.
            // Do not use ActiveFacilities or room occupancy (sleeping patients count).
            var facility = parent.TryGetComp<CompFacility>();
            if (facility == null)
                return true; // Fail open if a mod replaced facility behavior.
            foreach (var linked in facility.LinkedBuildings)
                if (linked is Building_Bed bed && bed.Spawned && bed.CurOccupants.Any())
                    return true;
            return false;
        }
        var now = Find.TickManager.TicksGame;
        return (intendedUntil >= now && intendedUntil - now <= IntentGraceTicks)
            || parent.Map.GetComponent<OccupancyMapComponent>().IsWorkTarget(parent, Kind == DeviceKind.ResearchBench);
    }

    internal float AdjustPowerDraw(float original)
    {
        // A tiny real standby load keeps vanilla power/job/facility gating valid.
        // Leave manual switches, outages, EMP and all power state transitions alone.
        if (original >= -1f || !Enabled || IsInUse())
            return original;
        return -1f;
    }

    public override string CompInspectStringExtra()
        => !Enabled ? null : IsInUse() ? "Power saving: normal power demand (in use)." : "Power saving: standby demand (1 W).";

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer
            || !(Kind == DeviceKind.MedicalMonitor ? AutoLight.Settings.AutomateMedicalMonitors : AutoLight.Settings.AutomateWorkbenches))
            yield break;
        yield return new Command_Toggle
        {
            icon = cachedIcon ??= ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true),
            defaultLabel = "Use idle power saving",
            defaultDesc = "Reduce idle demand to 1 W. Workbenches use normal power for selected or active work; vitals monitors use normal power while a linked bed is occupied. The manual power switch is unchanged.",
            isActive = () => !disabled,
            toggleAction = () => { disabled = !disabled; intendedUntil = -1; }
        };
    }
}

[HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.PowerOutput), MethodType.Getter)]
internal static class DevicePowerDrawPatch
{
    private static void Postfix(CompPowerTrader __instance, ref float __result)
    {
        if (__result < -1f && (AutoLight.Settings.AutomateWorkbenches || AutoLight.Settings.AutomateMedicalMonitors))
        {
            var automation = __instance.parent.TryGetComp<DeviceAutomation>();
            if (automation != null && __instance.GetType() == typeof(CompPowerTrader))
                __result = automation.AdjustPowerDraw(__result);
        }
    }
}

[HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
internal static class BillWorkIntentPatch
{
    private static void Postfix(Job __result)
        => (__result?.targetA.Thing as ThingWithComps)?.TryGetComp<DeviceAutomation>()?.NotifyWorkIntent(__result);
}

[HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.JobOnThing))]
internal static class ResearchWorkIntentPatch
{
    private static void Postfix(Job __result)
        => (__result?.targetA.Thing as ThingWithComps)?.TryGetComp<DeviceAutomation>()?.NotifyWorkIntent(__result);
}
