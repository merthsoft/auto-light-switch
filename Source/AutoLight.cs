using HarmonyLib;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff;

public class AutoLight : Mod
{
    public override string SettingsCategory() => "Auto Light Switch";
    public static AutoLightSettings Settings { get; private set; }
    private string onBuffer, offBuffer, delayBuffer;
    private Vector2 scrollPosition;

    public AutoLight(ModContentPack content) : base(content)
    {
        Settings = GetSettings<AutoLightSettings>();
        new Harmony("Merthsoft.AutoLightSwitch.1.7").PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var before = (Settings.OverrideCompProp, Settings.OverrideOnTicks, Settings.OverrideOffTicks,
            Settings.FastEntryDetection, Settings.VacancyDelayTicks, Settings.IgnoreAnimals, Settings.MuteAutomaticSwitching);
        var view = new Rect(0, 0, inRect.width - 20, 800);
        Widgets.BeginScrollView(inRect, ref scrollPosition, view);
        var ls = new Listing_Standard();
        ls.Begin(view);
        ls.CheckboxLabeled("Faster entry and doorway detection.", ref Settings.FastEntryDetection);
        ls.Label("When enabled, checks every 30 ticks and counts eligible pawns standing in doorways adjoining the room, including held-open doors.");
        ls.CheckboxLabeled("Ignore animals when checking room occupancy.", ref Settings.IgnoreAnimals);
        ls.CheckboxLabeled("Mute automatic power switching sounds.", ref Settings.MuteAutomaticSwitching);
        ls.Label("Manual flick sounds are unchanged.");
        ls.TextFieldNumericLabeled("Delay after the room becomes vacant (ticks).", ref Settings.VacancyDelayTicks, ref delayBuffer, 0, 2500);
        ls.Label("Zero keeps immediate shutoff at the next check. A positive delay starts when vacancy is first observed and restarts after eligible reentry. Vacancy is checked every 30 ticks while a delay is enabled.");
        ls.Gap();
        ls.CheckboxLabeled("Override check rate.", ref Settings.OverrideCompProp);
        if (Settings.OverrideCompProp)
        {
            ls.Label("Polling intervals in game ticks. Default: 250. Maximum: 2500 (one game hour). Faster detection or a vacancy delay uses a 30-tick cadence instead of these intervals.");
            ls.Label("With both features disabled, lamps normally tick every 250 ticks, so lower values cannot make them check faster.");
            ls.TextFieldNumericLabeled("Check rate when light is off.", ref Settings.OverrideOffTicks, ref offBuffer, 0, 2500);
            ls.TextFieldNumericLabeled("Check rate when light is on.", ref Settings.OverrideOnTicks, ref onBuffer, 0, 2500);
        }
        ls.End();
        Widgets.EndScrollView();
        var after = (Settings.OverrideCompProp, Settings.OverrideOnTicks, Settings.OverrideOffTicks,
            Settings.FastEntryDetection, Settings.VacancyDelayTicks, Settings.IgnoreAnimals, Settings.MuteAutomaticSwitching);
        if (before == after)
            return;
        Settings.Write();
        foreach (var map in Find.Maps)
            foreach (var thing in map.spawnedThings)
                thing.TryGetComp<Comp.ProximityPower>()?.ResetTimer(false);
    }
}
