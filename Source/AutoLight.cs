using Merthsoft.AutoOnAutoOff.Comp;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff {
    [StaticConstructorOnStartup]
    public class AutoLight : Mod {
        public override string SettingsCategory() => "Auto Light Switch";

        public static AutoLightSettings Settings { get; private set; }

        public AutoLight(ModContentPack content) : base(content) {
            Settings = GetSettings<AutoLightSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.CheckboxLabeled("Override check rate.", ref Settings.OverrideCompProp);

            if (Settings.OverrideCompProp) {
                ls.Label("These values are measured in in-game Ticks. The default is 250. Max is 2500, which is the number of ticks in one in-game hour.");

                ls.Label("(Though you can put a value under 250, that won't actually work, but the way minimum is handled by the UI is awful so I didn't want to set it.)");

                var offBuffer = Settings.OverrideOffTicks.ToString();
                ls.TextFieldNumericLabeled<int>("Check rate when light is off.", ref Settings.OverrideOffTicks, ref offBuffer, 0, 2500);

                var onBuffer = Settings.OverrideOnTicks.ToString();
                ls.TextFieldNumericLabeled<int>("Check rate when light is on.", ref Settings.OverrideOnTicks, ref onBuffer, 0, 2500);
            }

            ls.End();
            Settings.Write();

            Find.CurrentMap?.spawnedThings.ForEach(t => t.TryGetComp<ProximityPower>()?.ResetTimer(false));
        }
    }
}
