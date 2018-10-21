using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff.Comp {
    class ProximityPower : ThingComp {
        public CompProperties.ProximityPower Properties => (CompProperties.ProximityPower)this.props;
        protected int ticksUntilNextCheck = 0;
 
        public bool OverrideAutoPower = false;

        private Texture2D cachedCommandTex;
        private Texture2D CommandTex {
            get {
                if (this.cachedCommandTex == null) {
                    this.cachedCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true);
                }
                return this.cachedCommandTex;
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
                var room = parent.GetRoom();
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
                } else {

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
