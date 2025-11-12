using Celeste;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using Variant = ExtendedVariants.Module.ExtendedVariantsModule.Variant;
using static ExtendedVariants.Variants.DashRestriction;


namespace ExtendedVariants.Entities.ForMappers {

    [CustomEntity("ExtendedVariantMode/DashRestrictionTrigger")]
    public class DashRestrictionTrigger : AbstractExtendedVariantTrigger<DashRestrictionType> {
        public DashRestrictionTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        protected override Variant GetVariant(EntityData data) {
            return Variant.DashRestriction;
        }

        protected override DashRestrictionType GetNewValue(EntityData data) {
            return data.Enum("newValue", DashRestrictionType.None);
        }
    }
}
