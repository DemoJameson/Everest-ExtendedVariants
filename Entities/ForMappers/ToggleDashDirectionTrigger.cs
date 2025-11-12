using Celeste;
using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;

namespace ExtendedVariants.Entities.ForMappers {
    [CustomEntity("ExtendedVariantMode/ToggleDashDirectionTrigger")]
    public class ToggleDashDirectionTrigger : Trigger {
        private int dashDirection;
        private bool enable;
        private bool revertOnLeave;
        private bool revertOnDeath;
        private bool[][] newValue;

        // we are using bit fields for backwards compatibility, but this can otherwise be seen as an enum.
        private new const int Top = 0b1000000000;
        private new const int TopRight = 0b0100000000;
        private new const int Right = 0b0010000000;
        private new const int BottomRight = 0b0001000000;
        private new const int Bottom = 0b0000100000;
        private new const int BottomLeft = 0b0000010000;
        private new const int Left = 0b0000001000;
        private new const int TopLeft = 0b0000000100;

        public ToggleDashDirectionTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            // parse the trigger parameters
            dashDirection = data.Int("dashDirection", Top);
            enable = data.Bool("enable", true);
            revertOnLeave = data.Bool("revertOnLeave");
            revertOnDeath = data.Bool("revertOnDeath", true);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            bool[][] allowedDashDirections = (bool[][]) ExtendedVariantsModule.TriggerManager.GetCurrentVariantValue(ExtendedVariantsModule.Variant.DashDirection);

            // the new value is a copy of the old value with one boolean flipped.
            newValue = [new bool[3], new bool[3], new bool[3]];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    newValue[i][j] = allowedDashDirections[i][j];
                }
            }

            int x = 0, y = 0;
            switch (dashDirection) {
                case Top: x = 1; y = 0; break;
                case TopRight: x = 2; y = 0; break;
                case Right: x = 2; y = 1; break;
                case BottomRight: x = 2; y = 2; break;
                case Bottom: x = 1; y = 2; break;
                case BottomLeft: x = 0; y = 2; break;
                case Left: x = 0; y = 1; break;
                case TopLeft: x = 0; y = 0; break;
            }

            newValue[y][x] = enable;

            ExtendedVariantsModule.TriggerManager.OnEnteredInTrigger(ExtendedVariantsModule.Variant.DashDirection, newValue, revertOnLeave, isFade: false, revertOnDeath, legacy: false);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);

            if (revertOnLeave) {
                ExtendedVariantsModule.TriggerManager.OnExitedRevertOnLeaveTrigger(ExtendedVariantsModule.Variant.DashDirection, newValue, legacy: false);
            }
        }
    }
}
