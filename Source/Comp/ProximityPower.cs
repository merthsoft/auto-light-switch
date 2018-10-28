using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff.Comp {
    class ProximityPower : ThingComp {
        public CompProperties.ProximityPower Properties => (CompProperties.ProximityPower)props;
        protected int ticksUntilNextCheck = 0;
 
        public bool OverrideAutoPower = false;

        private Thing trueParent;
        public Thing TrueParent {
            get {
                if (trueParent == null) {
                    // Wall Lights have a "glower" field
                    trueParent = parent.GetType().GetFieldValue("glower", parent) as Thing;
                    // Wall Lights could also be the base class for the colored light
                    trueParent = trueParent ?? parent.GetType().BaseType.GetFieldValue("glower", parent) as Thing;
                    // Otherwise, just ues the parent
                    trueParent = trueParent ?? parent;
                }
                return trueParent;
            }
        }

        private Texture2D cachedCommandTex;
        private Texture2D CommandTex {
            get {
                if (cachedCommandTex == null) {
                    cachedCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true);
                }
                return cachedCommandTex;
            }
        }

        public override void PostExposeData() {
            base.PostExposeData();

            Scribe_Values.Look<bool>(ref OverrideAutoPower, "OverrideAutoPower");
        }

        public override void CompTick() {
            handleTicks(1);
        }

        public override void CompTickRare() {
            handleTicks(GenTicks.TickRareInterval);
        }

        public void ResetTimer() {
            ticksUntilNextCheck = Properties.checkRate;
        }

        private void handleTicks(int ticks) {
            if (OverrideAutoPower) { return; }

            var flickComp = parent.TryGetComp<CompFlickable>();
            
            ticksUntilNextCheck -= ticks;
            if (ticksUntilNextCheck <= 0) {
                var room = TrueParent.GetRoom();
                if (room == null) { return; }

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
                if (occupied && Properties.autoOn) {
                    lightOn = true;
                } else if (!occupied && Properties.autoOff) {
                    lightOn = false;
                }

                if (flickComp != null) {
                    flickComp.SwitchIsOn = lightOn;
                }

                ResetTimer();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            foreach (Gizmo c in base.CompGetGizmosExtra()) {
                yield return c;
            }

            if (Properties.showOverrideButton && this.parent.Faction == Faction.OfPlayer) {
                yield return new Command_Toggle {
                    icon = CommandTex,
                    defaultLabel = "Override auto switch",
                    defaultDesc = "Toggles whether the power is automatically on or off.",
                    isActive = (() => OverrideAutoPower),
                    toggleAction = delegate {
                        OverrideAutoPower = !OverrideAutoPower;
                    }
                };
            }
        }
    }
}
