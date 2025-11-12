using Celeste;
using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;

namespace ExtendedVariants.Entities.ForMappers {
    [CustomEntity("ExtendedVariantMode/DashDirectionTrigger")]
    public class DashDirectionTrigger : AbstractExtendedVariantTrigger<bool[][]> {
        public DashDirectionTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        protected override ExtendedVariantsModule.Variant GetVariant(EntityData data) {
            return ExtendedVariantsModule.Variant.DashDirection;
        }

        protected override bool[][] GetNewValue(EntityData data) {
            return [
                [data.Bool("topLeft"), data.Bool("top"), data.Bool("topRight")],
                [data.Bool("left"), true, data.Bool("right")],
                [data.Bool("bottomLeft"), data.Bool("bottom"), data.Bool("bottomRight")]
            ];
        }
    }
}
