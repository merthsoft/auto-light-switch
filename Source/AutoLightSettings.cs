using Verse;

namespace Merthsoft.AutoOnAutoOff {
    public class AutoLightSettings : ModSettings {
        public bool OverrideCompProp;
        public int OverrideOnTicks;
        public int OverrideOffTicks;

        public override void ExposeData() {
            base.ExposeData();

            Scribe_Values.Look(ref OverrideCompProp, nameof(OverrideCompProp), false);
            Scribe_Values.Look(ref OverrideOnTicks, nameof(OverrideOnTicks), 250);
            Scribe_Values.Look(ref OverrideOffTicks, nameof(OverrideOffTicks), 250);
        }
    }
}
