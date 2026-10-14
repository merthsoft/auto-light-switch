using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Merthsoft.AutoOnAutoOff;

internal static class AutomaticSwitchSounds
{
    private sealed class Pending
    {
        public bool On;
        public int Expires;
    }

    private static readonly ConditionalWeakTable<CompPowerTrader, Pending> pending = new();

    internal static void Mark(CompPowerTrader power, bool on)
    {
        Cancel(power);
        if (power == null || !AutoLight.Settings.MuteAutomaticSwitching)
            return;
        // Power-on is applied later by the power net, unlike synchronous flick-off.
        pending.Add(power, new Pending { On = on, Expires = Find.TickManager.TicksGame + GenTicks.TickRareInterval });
    }

    internal static void Cancel(CompPowerTrader power)
    {
        if (power != null)
            pending.Remove(power);
    }

    internal static void PlayPowerSound(SoundDef sound, SoundInfo info, CompPowerTrader power)
    {
        if (pending.TryGetValue(power, out var change) && power.PowerOn == change.On)
        {
            Cancel(power);
            if (AutoLight.Settings.MuteAutomaticSwitching && Find.TickManager.TicksGame <= change.Expires)
                return;
        }
        sound.PlayOneShot(info);
    }
}

// Replace only the two transition sounds in the vanilla power setter. Do not
// suppress global sound playback, powered sustainers, signals, or manual flicks.
[HarmonyPatch(typeof(CompPowerTrader), nameof(CompPowerTrader.PowerOn), MethodType.Setter)]
internal static class PowerTransitionSoundPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var play = AccessTools.Method(typeof(SoundStarter), nameof(SoundStarter.PlayOneShot), new[] { typeof(SoundDef), typeof(SoundInfo) });
        var replacement = AccessTools.Method(typeof(AutomaticSwitchSounds), nameof(AutomaticSwitchSounds.PlayPowerSound));
        var replacements = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(play))
            {
                var load = new CodeInstruction(OpCodes.Ldarg_0);
                load.labels.AddRange(instruction.labels);
                instruction.labels.Clear();
                load.blocks.AddRange(instruction.blocks);
                instruction.blocks.Clear();
                yield return load;
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replacements++;
            }
            yield return instruction;
        }
        if (replacements != 2)
            Log.Warning($"Auto Light Switch expected two power transition sound calls; found {replacements}. Check compatibility with this game version or other power patches.");
    }
}

[HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.DoFlick))]
internal static class ManualFlickSoundPatch
{
    private static void Prefix(CompFlickable __instance)
        => AutomaticSwitchSounds.Cancel(__instance.parent.TryGetComp<CompPowerTrader>());
}
