﻿using Celeste;
using Celeste.Mod;
using MonoMod.Cil;
using System;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class InvertGrab : AbstractExtendedVariant {

        private ILHook dashCoroutineHook;

        public InvertGrab() : base(variantType: typeof(bool), defaultVariantValue: false) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value != 0;
        }

        public override void Load() {
            IL.Celeste.Player.NormalUpdate += modInputGrabCheck;
            IL.Celeste.Player.ClimbUpdate += modInputGrabCheck;
            IL.Celeste.Player.DashUpdate += modInputGrabCheck;
            IL.Celeste.Player.DashCoroutine += modInputGrabCheck;
            IL.Celeste.Player.SwimUpdate += modInputGrabCheck;
            IL.Celeste.Player.RedDashUpdate += modInputGrabCheck;
            IL.Celeste.Player.HitSquashUpdate += modInputGrabCheck;
            IL.Celeste.Player.LaunchUpdate += modInputGrabCheck;
            IL.Celeste.Player.DreamDashUpdate += modInputGrabCheck;
            IL.Celeste.Player.StarFlyUpdate += modInputGrabCheck;

            dashCoroutineHook = new ILHook(typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), modInputGrabCheck);
        }

        public override void Unload() {
            IL.Celeste.Player.NormalUpdate -= modInputGrabCheck;
            IL.Celeste.Player.ClimbUpdate -= modInputGrabCheck;
            IL.Celeste.Player.DashUpdate -= modInputGrabCheck;
            IL.Celeste.Player.DashCoroutine -= modInputGrabCheck;
            IL.Celeste.Player.SwimUpdate -= modInputGrabCheck;
            IL.Celeste.Player.RedDashUpdate -= modInputGrabCheck;
            IL.Celeste.Player.HitSquashUpdate -= modInputGrabCheck;
            IL.Celeste.Player.LaunchUpdate -= modInputGrabCheck;
            IL.Celeste.Player.DreamDashUpdate -= modInputGrabCheck;
            IL.Celeste.Player.StarFlyUpdate -= modInputGrabCheck;

            if (dashCoroutineHook != null) dashCoroutineHook.Dispose();
        }

        private void modInputGrabCheck(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // mod all Input.Grab.Check
            while (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdsfld(typeof(Input), "Grab"),
                instr => instr.MatchCallvirt<VirtualButton>("get_Check")
            )) {
                Logger.Log("ExtendedVariantMode/InvertGrab", $"Adding code to apply Invert Grab at index {cursor.Index} in CIL code for Player.{cursor.Method.Name}");
                cursor.GotoNext().Remove().EmitDelegate<Func<VirtualButton, bool>>(invertButtonCheck);
            }

            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(typeof(Input), "get_GrabCheck"))) {
                Logger.Log("ExtendedVariantMode/InvertGrab", $"Adding code to apply Invert Grab at index {cursor.Index} in CIL code for Player.{cursor.Method.Name}");
                cursor.GotoNext().EmitDelegate<Func<bool, bool>>(invertButtonCheck);
            }
        }

        private bool invertButtonCheck(VirtualButton button) {
            return GetVariantValue<bool>(Variant.InvertGrab) ? !button.Check : button.Check;
        }

        private bool invertButtonCheck(bool buttonCheck) {
            return GetVariantValue<bool>(Variant.InvertGrab) ? !buttonCheck : buttonCheck;
        }
    }
}