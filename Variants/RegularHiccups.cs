﻿using System;
using Celeste;
using Celeste.Mod;
using ExtendedVariants.Module;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class RegularHiccups : AbstractExtendedVariant {

        private float regularHiccupTimer = 0f;

        public RegularHiccups() : base(variantType: typeof(float), defaultVariantValue: 0f) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value / 10f;
        }

        public override void Load() {
            On.Celeste.Player.Added += onPlayerAdded;
            On.Celeste.Player.Update += modUpdate;
            IL.Celeste.Player.HiccupJump += modHiccupJump;
        }

        public override void Unload() {
            On.Celeste.Player.Added -= onPlayerAdded;
            On.Celeste.Player.Update -= modUpdate;
            IL.Celeste.Player.HiccupJump -= modHiccupJump;
        }

        public override void VariantValueChanged() {
            regularHiccupTimer = GetVariantValue<float>(Variant.RegularHiccups);
        }

        private void onPlayerAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene) {
            orig(self, scene);

            // reset the hiccup timer when the player respawns, for more consistency.
            regularHiccupTimer = GetVariantValue<float>(Variant.RegularHiccups);
        }

        private void modUpdate(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            if (GetVariantValue<float>(Variant.RegularHiccups) != 0f) {
                regularHiccupTimer -= Engine.DeltaTime;

                if (regularHiccupTimer > GetVariantValue<float>(Variant.RegularHiccups)) {
                    regularHiccupTimer = GetVariantValue<float>(Variant.RegularHiccups);
                }
                if (regularHiccupTimer <= 0) {
                    regularHiccupTimer = GetVariantValue<float>(Variant.RegularHiccups);
                    self.HiccupJump();
                }
            }
        }

        private void modHiccupJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-60f))) {
                Logger.Log("ExtendedVariantMode/RegularHiccups", $"Modding hiccup size at {cursor.Index} in CIL code for HiccupJump");

                cursor.EmitDelegate<Func<float>>(determineHiccupStrengthFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private float determineHiccupStrengthFactor() {
            return GetVariantValue<float>(Variant.HiccupStrength);
        }
    }
}
