﻿using Celeste;
using Monocle;
using System;

namespace ExtendedVariants.Variants.Vanilla {
    public class AirDashes : AbstractVanillaVariant {
        public AirDashes() : base(variantType: typeof(Assists.DashModes), defaultVariantValue: Assists.DashModes.Normal) { }

        public override object ConvertLegacyVariantValue(int value) {
            return (Assists.DashModes) value;
        }

        public override void VariantValueChanged() {
            Assists.DashModes dashMode = getActiveAssistValues().DashMode;

            Player player = Engine.Scene?.Tracker.GetEntity<Player>();
            if (player != null) {
                player.Dashes = Math.Min(player.Dashes, dashMode != Assists.DashModes.Normal ? 2 : player.MaxDashes);
            }
        }

        protected override Assists applyVariantValue(Assists target, object value) {
            target.DashMode = (Assists.DashModes) value;
            return target;
        }
    }
}
