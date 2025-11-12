using Celeste;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace ExtendedVariants.Entities.ForMappers {
    public class AbstractExtendedVariantTriggerTeleportHandler {
        // === hook on teleport to sync up a variant change with a teleport
        // since all teleport triggers call UnloadLevel(), we can hook that to detect the instant the teleport happens at.
        // this is in a separate class because we don't want it for each generic type.

        internal static event Action OnTeleport;

        public static void Load() {
            On.Celeste.Level.UnloadLevel += onUnloadLevel;
        }

        public static void Unload() {
            On.Celeste.Level.UnloadLevel -= onUnloadLevel;
            OnTeleport = null;
        }

        private static void onUnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
            if (OnTeleport != null) {
                OnTeleport();
                OnTeleport = null;
            }

            orig(self);
        }
    }

    public abstract class AbstractExtendedVariantTrigger<T> : Trigger {
        private ExtendedVariantsModule.Variant variantChange;
        private Func<T> newValueGetter;
        private T valueToRevertOnLeave;
        private bool revertOnLeave;
        private bool revertOnDeath;
        private bool delayRevertOnDeath;
        private bool withTeleport;
        private bool coversScreen;
        private bool onlyOnce;

        public AbstractExtendedVariantTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            // parse the trigger parameters
            variantChange = GetVariant(data);
            newValueGetter = () => GetNewValue(data);
            revertOnLeave = data.Bool("revertOnLeave");
            revertOnDeath = data.Bool("revertOnDeath", true);
            delayRevertOnDeath = data.Bool("delayRevertOnDeath");
            withTeleport = data.Bool("withTeleport");
            coversScreen = data.Bool("coversScreen");
            onlyOnce = data.Bool("onlyOnce");

            if (!data.Bool("enable", true)) {
                // "disabling" a variant is actually just resetting its value to default
                newValueGetter = () => (T) ExtendedVariantTriggerManager.GetDefaultValueForVariant(variantChange);
            }

            valueToRevertOnLeave = newValueGetter();

            // this is a replacement for the Flag-Toggled Extended Variant Trigger.
            if (!string.IsNullOrEmpty(data.Attr("flag"))) {
                Add(new FlagToggleComponent(data.Attr("flag"), data.Bool("flagInverted")));
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            Rectangle bounds = SceneAs<Level>().Bounds;

            // this is a replacement for the Extended Variant Controller.
            if (coversScreen) {
                // the trigger should stick out on the top because the player can go offscreen by up to 24px when there is no screen above.
                Position = new Vector2(bounds.X, bounds.Y - 24f);
                Collider.Width = bounds.Width;
                Collider.Height = bounds.Height + 32f;
            }
        }

        protected virtual ExtendedVariantsModule.Variant GetVariant(EntityData data) {
            return data.Enum("variantChange", ExtendedVariantsModule.Variant.Gravity);
        }

        protected abstract T GetNewValue(EntityData data);

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            Action applyVariant = () => {
                T value = newValueGetter();
                ExtendedVariantsModule.TriggerManager.OnEnteredInTrigger(variantChange, value, revertOnLeave, isFade: false, revertOnDeath, legacy: false);
                valueToRevertOnLeave = value;
            };

            if (withTeleport) {
                AbstractExtendedVariantTriggerTeleportHandler.OnTeleport += applyVariant;
            } else {
                applyVariant();
            }

            if (onlyOnce) {
                RemoveSelf();
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);

            if (revertOnLeave && (!delayRevertOnDeath || !player.Dead)) {
                ExtendedVariantsModule.TriggerManager.OnExitedRevertOnLeaveTrigger(variantChange, valueToRevertOnLeave, legacy: false);
            }
        }

        // This comes from the flag-toggled camera triggers in Maddie's Helping Hand, which come from the Spring Collab.
        // Recycling is good.
        internal class FlagToggleComponent : Component {
            public bool Enabled = true;
            private string flag;
            private Action onDisable;
            private Action onEnable;
            private bool inverted;

            public FlagToggleComponent(string flag, bool inverted, Action onDisable = null, Action onEnable = null) : base(true, false) {
                this.flag = flag;
                this.inverted = inverted;
                this.onDisable = onDisable;
                this.onEnable = onEnable;
            }

            public override void EntityAdded(Scene scene) {
                base.EntityAdded(scene);
                UpdateFlag();
            }

            public override void Update() {
                base.Update();
                UpdateFlag();
            }

            public void UpdateFlag() {
                if ((!inverted && SceneAs<Level>().Session.GetFlag(flag) != Enabled)
                    || (inverted && SceneAs<Level>().Session.GetFlag(flag) == Enabled)) {

                    if (Enabled) {
                        // disable the entity.
                        Entity.Visible = Entity.Collidable = false;
                        onDisable?.Invoke();
                        Enabled = false;
                    } else {
                        // enable the entity.
                        Entity.Visible = Entity.Collidable = true;
                        onEnable?.Invoke();
                        Enabled = true;
                    }
                }
            }
        }
    }
}
