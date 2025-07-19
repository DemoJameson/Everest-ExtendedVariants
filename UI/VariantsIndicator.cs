﻿using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using ExtendedVariants.Module;
using System.Linq;
using static ExtendedVariants.Module.ExtendedVariantsModule;

// UI class for clear verification purposes.
namespace ExtendedVariants.UI {
    public class VariantsIndicator {
        private MTexture indicatorSprite = null;

        private float opacity = 0.15f;

        private bool hasPlayerOverrideVariant = false;

        private Vector2 uiPos = new Vector2(20, 229);
        private Vector2 origin = new Vector2(0, 0);

        public static readonly ExtendedVariantsModule.Variant[] WatermarkedVariants = new ExtendedVariantsModule.Variant[] {
            Variant.UltraProtection,
            Variant.LiftboostProtection,
            Variant.CornerboostProtection,
            Variant.CrouchDashFix,
            Variant.AlternativeBuffering,
            Variant.SaferDiagonalSmuggle,
            Variant.DashBeforePickup,
            Variant.ThrowIgnoresForcedMove,
            Variant.MultiBuffering,
            Variant.MidairTech, // Could be used to fudge a super
            Variant.ConsistentThrowing
        };

        public void Update(IEnumerable<ExtendedVariantsModule.Variant> userSettings) {
            hasPlayerOverrideVariant = userSettings.Intersect(WatermarkedVariants).Any();
        }

        public void Render() {
            if (hasPlayerOverrideVariant) {
                indicatorSprite ??= GFX.Gui["ExtendedVariantMode/complete_screen_stamp"];
                indicatorSprite.Draw(uiPos, origin, Color.White * opacity, 0.5f);
            }
        }
    }
}
