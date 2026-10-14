using Verse;

namespace Merthsoft.AutoOnAutoOff;

public class AutoLightSettings : ModSettings
{
    public bool OverrideCompProp;
    public int OverrideOnTicks = 250;
    public int OverrideOffTicks = 250;
    public bool AutomateWorkbenches;
    public bool AutomateMedicalMonitors;
    public bool FastEntryDetection;
    public int VacancyDelayTicks;
    public bool IgnoreAnimals;
    public bool MuteAutomaticSwitching;

    public bool NeedsFrequentChecks => FastEntryDetection || VacancyDelayTicks > 0;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OverrideCompProp, nameof(OverrideCompProp), false);
        Scribe_Values.Look(ref OverrideOnTicks, nameof(OverrideOnTicks), 250);
        Scribe_Values.Look(ref OverrideOffTicks, nameof(OverrideOffTicks), 250);
        Scribe_Values.Look(ref AutomateWorkbenches, nameof(AutomateWorkbenches), false);
        Scribe_Values.Look(ref AutomateMedicalMonitors, nameof(AutomateMedicalMonitors), false);
        Scribe_Values.Look(ref FastEntryDetection, nameof(FastEntryDetection), false);
        Scribe_Values.Look(ref VacancyDelayTicks, nameof(VacancyDelayTicks), 0);
        Scribe_Values.Look(ref IgnoreAnimals, nameof(IgnoreAnimals), false);
        Scribe_Values.Look(ref MuteAutomaticSwitching, nameof(MuteAutomaticSwitching), false);
    }
}
