﻿using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using static ExtendedVariants.Module.ExtendedVariantsModule;
using MonoMod.RuntimeDetour;

namespace ExtendedVariants.Variants {
    public class NoFreezeFrames : AbstractExtendedVariant {

        public NoFreezeFrames() : base(variantType: typeof(bool), defaultVariantValue: false) { }

        public override object ConvertLegacyVariantValue(int value) {
            return value != 0;
        }

        public override void Load() {
            On.Monocle.Engine.Update += onEngineUpdate;

            // this one cuts off calls to orig, so we want to make sure it as close to vanilla as possible.
            using (new DetourConfigContext(new DetourConfig("BeforeAll").AddBefore("*")).Use()) {
                On.Celeste.Celeste.Freeze += onCelesteFreeze;
            }
        }

        public override void Unload() {
            On.Monocle.Engine.Update -= onEngineUpdate;
            On.Celeste.Celeste.Freeze -= onCelesteFreeze;
        }

        private void onCelesteFreeze(On.Celeste.Celeste.orig_Freeze orig, float time) {
            if (GetVariantValue<bool>(Variant.NoFreezeFrames)) {
                if (GetVariantValue<bool>(Variant.NoFreezeFramesAdvanceCassetteBlocks)) {
                    Engine.Scene?.Tracker.GetEntity<CassetteBlockManager>()?.AdvanceMusic(time);
                }

                return;
            }
            orig(time);
        }

        private void onEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            if (GetVariantValue<bool>(Variant.NoFreezeFrames) && Engine.FreezeTimer > 0f) {
                if (GetVariantValue<bool>(Variant.NoFreezeFramesAdvanceCassetteBlocks)) {
                    Engine.Scene?.Tracker.GetEntity<CassetteBlockManager>()?.AdvanceMusic(Engine.FreezeTimer);
                }

                Engine.FreezeTimer = 0f;
            }

            orig(self, gameTime);
        }
    }
}
