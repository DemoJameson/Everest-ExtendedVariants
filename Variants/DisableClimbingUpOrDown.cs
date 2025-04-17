﻿using Celeste;
using Celeste.Mod;
using Monocle;
using MonoMod.Cil;
using System;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class DisableClimbingUpOrDown : AbstractExtendedVariant {
        public enum ClimbUpOrDownOptions { Disabled, Up, Down, Both }

        public DisableClimbingUpOrDown() : base(variantType: typeof(ClimbUpOrDownOptions), defaultVariantValue: ClimbUpOrDownOptions.Disabled) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value == 0 ? ClimbUpOrDownOptions.Disabled : ClimbUpOrDownOptions.Both;
        }

        public override void Load() {
            IL.Celeste.Player.ClimbUpdate += onPlayerClimbUpdate;
        }

        public override void Unload() {
            IL.Celeste.Player.ClimbUpdate -= onPlayerClimbUpdate;
        }

        private void onPlayerClimbUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdsfld(typeof(Input), "MoveY"),
                instr => instr.MatchLdfld<VirtualIntegerAxis>("Value"))) {

                Logger.Log("ExtendedVariantMode/DisableClimbingUpOrDown", $"Modifying MoveY to prevent player from moving @ {cursor.Index} in IL for Player.ClimbUpdate");
                cursor.EmitDelegate<Func<int, int>>(orig => {
                    switch (GetVariantValue<ClimbUpOrDownOptions>(Variant.DisableClimbingUpOrDown)) {
                        case ClimbUpOrDownOptions.Both:
                            return 0;
                        case ClimbUpOrDownOptions.Up:
                            return Math.Max(0, orig);
                        case ClimbUpOrDownOptions.Down:
                            return Math.Min(0, orig);
                        default:
                            return orig;
                    }
                });
            }
        }
    }
}
