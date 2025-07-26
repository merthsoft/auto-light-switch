using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.AutoOnAutoOff.Comp;

internal class ProximityPower : ThingComp
{
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
        => cachedCommandTex ??= ContentFinder<Texture2D>.Get("UI/Commands/OverrideButton", true);

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look<bool>(ref OverrideAutoPower, nameof(OverrideAutoPower));
    }

    public override void CompTick() 
        => HandleTicks(1);

    public override void CompTickRare() 
        => HandleTicks(GenTicks.TickRareInterval);

    public void ResetTimer(bool lightOn) 
        => ticksUntilNextCheck = !AutoLight.Settings.OverrideCompProp 
            ? Properties.checkRate 
            : lightOn ? AutoLight.Settings.OverrideOnTicks : AutoLight.Settings.OverrideOffTicks;

    public Room GetRoom()
        => TrueParent.GetRoom() ?? Compatability.MURWallLight.GetRoom(parent);

    private void HandleTicks(int ticks)
    {
        if (OverrideAutoPower)
            return;

        ticksUntilNextCheck -= ticks;
        if (ticksUntilNextCheck <= 0)
        {
            var room = GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
                return;

            var occupied = false;
            foreach (var thing in room.ContainedAndAdjacentThings)
            {
                if (thing is not Pawn pawn)
                    continue;
                
                if (pawn.GetRoom()?.ID != room.ID)
                    continue;
                
                if (!Properties.offWhenSleeping || pawn.Awake())
                {
                    occupied = true;
                    break;
                }
            }

            var lightOn = false;
            if (occupied && Properties.autoOn)
                lightOn = true;
            else if (!occupied && Properties.autoOff)
                lightOn = false;

            setSwitchIsOn(lightOn);
            ResetTimer(lightOn);
        }

        void setSwitchIsOn(bool switchIsOn)
        {
            var flickComp = parent.TryGetComp<CompFlickable>();
            flickComp?.SwitchIsOn = switchIsOn;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var c in base.CompGetGizmosExtra())
            yield return c;

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
                    if (OverrideAutoPower)
                        parent.TryGetComp<CompFlickable>()?.ResetToOn();
                }
            };
    }
}
