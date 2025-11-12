using Celeste;
using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Entities.ForMappers {
    [CustomEntity("ExtendedVariantMode/BooleanExtendedVariantTrigger", "ExtendedVariantMode/BooleanVanillaVariantTrigger")]
    public class BooleanExtendedVariantTrigger : AbstractExtendedVariantTrigger<bool> {
        private readonly Variant variantChange;
        private readonly bool toggle;

        public BooleanExtendedVariantTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            variantChange = GetVariant(data);
            toggle = data.Bool("toggle");
        }

        protected override bool GetNewValue(EntityData data) {
            if (toggle) {
                return !(bool) ExtendedVariantsModule.TriggerManager.GetCurrentVariantValue(variantChange);
            }

            return data.Bool("newValue");
        }
    }
}
