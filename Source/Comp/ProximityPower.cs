using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff.Comp {
    class ProximityPower : ThingComp {
        public CompProperties.ProximityPower Properties => (CompProperties.ProximityPower)props;
        protected int ticksUntilNextCheck = 0;
 
        public bool OverrideAutoPower = false;
       
        private Thing trueParent;
        
        public Thing TrueParent
            => trueParent ??= parent.GetFieldValue<Thing>("glower")
                ?? parent.GetType().BaseType.GetFieldValue<Thing>("glower", parent)
                ?? parent;

        private Texture2D cachedCommandTex;
        private Texture2D CommandTex 
            => cachedCommandTex ?? (cachedCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true));

        public override void PostExposeData() {
            base.PostExposeData();

            Scribe_Values.Look<bool>(ref OverrideAutoPower, "OverrideAutoPower");
        }

        public override void CompTick() 
            => handleTicks(1);

        public override void CompTickRare() 
            => handleTicks(GenTicks.TickRareInterval);

        public void ResetTimer(bool lightOn) 
            => ticksUntilNextCheck = !AutoLight.Settings.OverrideCompProp 
                ? Properties.checkRate 
                : lightOn ? AutoLight.Settings.OverrideOnTicks : AutoLight.Settings.OverrideOffTicks;

        public Room GetRoom()
            => TrueParent.GetRoom() ?? Compatability.MURWallLight.GetRoom(parent);

        private void handleTicks(int ticks) {
            if (OverrideAutoPower) { return; }
                       
            ticksUntilNextCheck -= ticks;
            if (ticksUntilNextCheck <= 0) {
                var room = GetRoom();
                if (room == null || room.PsychologicallyOutdoors)
                    return; 
                
                var occupied = false;
                foreach (var cell in room.Cells) {
                    var pawns = cell.GetThingList(parent.Map).Where(t => t is Pawn).Cast<Pawn>();
                    if (!Properties.offWhenSleeping) {
                        if (pawns.Count() > 0) {
                            occupied = true;
                            break;
                        }
                    } else {
                        if (pawns.Count(p => p.Awake()) > 0) {
                            occupied = true;
                            break;
                        }
                    }
                }

                bool lightOn = false;
                if (occupied && Properties.autoOn)
                    lightOn = true;
                else if (!occupied && Properties.autoOff)
                    lightOn = false;

                setSwitchIsOn(lightOn);
                ResetTimer(lightOn);
            }
        }

        private void setSwitchIsOn(bool switchIsOn) {
            var flickComp = parent.TryGetComp<CompFlickable>();
            if (flickComp != null) 
                flickComp.SwitchIsOn = switchIsOn;
        }

        private void resetToOn() {
            var flickComp = parent.TryGetComp<CompFlickable>();
            if (flickComp != null) 
                flickComp.ResetToOn();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            foreach (Gizmo c in base.CompGetGizmosExtra())
                yield return c;

            if (Properties.showOverrideButton && parent.Faction == Faction.OfPlayer) {
                yield return new Command_Toggle {
                    icon = CommandTex,
                    defaultLabel = "Override auto switch",
                    defaultDesc = "Toggles whether the power is automatically on or off.",
                    isActive = (() => OverrideAutoPower),
                    toggleAction = delegate {
                        if (OverrideAutoPower = !OverrideAutoPower) {
                            resetToOn();
                        }
                    }
                };
            }
        }
    }
}
