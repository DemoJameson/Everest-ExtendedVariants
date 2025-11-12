using ExtendedVariants.Module;
using System;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class LegacyDashSpeedBehavior : AbstractExtendedVariant {
        public LegacyDashSpeedBehavior() : base(variantType: typeof(bool), defaultVariantValue: false) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value != 0;
        }

        public override void VariantValueChanged() {
            if ((ExtendedVariantsModule.VariantHandlers[Variant.DashSpeed] is DashSpeedOld) != GetVariantValue<bool>(Variant.LegacyDashSpeedBehavior)) {
                // hot swap the "dash speed" variant handler
                ExtendedVariantsModule.VariantHandlers[Variant.DashSpeed].Unload();
                ExtendedVariantsModule.VariantHandlers[Variant.DashSpeed] = GetVariantValue<bool>(Variant.LegacyDashSpeedBehavior) ? (AbstractExtendedVariant) new DashSpeedOld() : new DashSpeed();
                ExtendedVariantsModule.VariantHandlers[Variant.DashSpeed].Load();
            }
        }
    }
}
