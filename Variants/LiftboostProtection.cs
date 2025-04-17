﻿using System;
using System.Reflection;
using Celeste;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Platform = Celeste.Platform;

namespace ExtendedVariants.Variants {
    public class LiftboostProtection : AbstractExtendedVariant {
        private static bool TryGetPlatform(Player player, Vector2 dir, out Platform platform) {
            if (dir.X == 0f && dir.Y > 0f)
                platform = player.CollideFirst<Platform>(player.Position + dir);
            else
                platform = player.CollideFirst<Solid>(player.Position + dir);

            return platform != null;
        }

        private static float CorrectLiftSpeed(float minusTwo, float minusOne, float liftSpeed)
            => Math.Sign(liftSpeed) * Math.Max(Math.Abs(liftSpeed), Math.Min(Math.Sign(liftSpeed) * minusOne, Math.Sign(liftSpeed) * (2f * minusOne - minusTwo)));

        private ILHook il_Celeste_Player_Orig_WallJump;

        public override void Load() {
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.SuperJump += Player_SuperJump;
            il_Celeste_Player_Orig_WallJump
                = new ILHook(typeof(Player).GetMethod("orig_WallJump", BindingFlags.NonPublic | BindingFlags.Instance), Player_orig_WallJump_il);
            On.Celeste.Player.SuperWallJump += Player_SuperWallJump;
            On.Celeste.Platform.Update += Platform_Update;
            IL.Celeste.Platform.MoveH_float += PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveH_float_float += PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveV_float += PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveV_float_float += PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHNaive += PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVNaive += PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHCollideSolids += PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVCollideSolids += PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHCollideSolidsAndBounds += PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVCollideSolidsAndBounds_Level_float_bool_Action3_bool += PatchLiftboostProtectionY;
        }

        public override void Unload() {
            On.Celeste.Player.Jump -= Player_Jump;
            On.Celeste.Player.SuperJump -= Player_SuperJump;
            il_Celeste_Player_Orig_WallJump.Dispose();
            On.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
            On.Celeste.Platform.Update -= Platform_Update;
            IL.Celeste.Platform.MoveH_float -= PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveH_float_float -= PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveV_float -= PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveV_float_float -= PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHNaive -= PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVNaive -= PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHCollideSolids -= PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVCollideSolids -= PatchLiftboostProtectionY;
            IL.Celeste.Platform.MoveHCollideSolidsAndBounds -= PatchLiftboostProtectionX;
            IL.Celeste.Platform.MoveVCollideSolidsAndBounds_Level_float_bool_Action3_bool -= PatchLiftboostProtectionY;
        }

        public LiftboostProtection() : base(variantType: typeof(bool), defaultVariantValue: false) { }

        public override object ConvertLegacyVariantValue(int value) => value != 0;

        private void Player_Jump(On.Celeste.Player.orig_Jump jump, Player player, bool particles, bool playsfx) {
            if (GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)
                && player.LiftSpeed == Vector2.Zero && TryGetPlatform(player, Vector2.UnitY, out var platform)) {
                var safeLiftSpeed = DynamicData.For(platform).Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                if (platform is not JumpThru || safeLiftSpeed.Y != 0f)
                    player.LiftSpeed = safeLiftSpeed;
            }

            jump(player, particles, playsfx);
        }

        private void Player_SuperJump(On.Celeste.Player.orig_SuperJump superJump, Player player) {
            if (GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)
                && player.LiftSpeed == Vector2.Zero && TryGetPlatform(player, Vector2.UnitY, out var platform)) {
                var safeLiftSpeed = DynamicData.For(platform).Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                if (platform is not JumpThru || safeLiftSpeed.Y != 0f)
                    player.LiftSpeed = safeLiftSpeed;
            }

            superJump(player);
        }

        private void Player_orig_WallJump_il(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchCall<Actor>("set_LiftSpeed"));

            cursor.Emit(OpCodes.Ldloc_2);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, Solid, int, Vector2>>((value, solid, dir) => {
                if (!GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection))
                    return value;

                var safeLiftSpeed = DynamicData.For(solid).Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                if (Math.Sign(safeLiftSpeed.X) == dir)
                    return safeLiftSpeed.X * Vector2.UnitX;

                return value;
            });
        }

        private void Player_SuperWallJump(On.Celeste.Player.orig_SuperWallJump superWallJump, Player player, int dir) {
            if (GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)
                && player.LiftSpeed == Vector2.Zero && TryGetPlatform(player, -5 * dir * Vector2.UnitX, out var platform)) {
                var safeLiftSpeed = DynamicData.For(platform).Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                if (Math.Sign(safeLiftSpeed.X) == dir)
                    player.LiftSpeed = safeLiftSpeed.X * Vector2.UnitX;
            }

            superWallJump(player, dir);
        }

        private void Platform_Update(On.Celeste.Platform.orig_Update update, Platform platform) {
            if (GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)) {
                var dynamicData = DynamicData.For(platform);

                dynamicData.Set("safeLiftSpeedMinusTwo", dynamicData.Get<Vector2?>("safeLiftSpeedMinusOne") ?? Vector2.Zero);
                dynamicData.Set("safeLiftSpeedMinusOne", dynamicData.Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero);
                dynamicData.Set("safeLiftSpeed", Vector2.Zero);
            }

            update(platform);
        }

        private void PatchLiftboostProtectionX(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.Index = -1;
            cursor.GotoPrev(instr => instr.MatchLdflda<Platform>("LiftSpeed"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchStfld<Vector2>("X"));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Platform>>(platform => {
                if (!GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection))
                    return;

                float liftSpeed = platform.LiftSpeed.X;

                if (liftSpeed == 0f)
                    return;

                var dynamicData = DynamicData.For(platform);
                float minusTwo = dynamicData.Get<Vector2?>("safeLiftSpeedMinusTwo")?.X ?? 0f;
                float minusOne = dynamicData.Get<Vector2?>("safeLiftSpeedMinusOne")?.X ?? 0f;

                platform.LiftSpeed.X = CorrectLiftSpeed(minusTwo, minusOne, liftSpeed);

                var safeLiftSpeed = dynamicData.Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                safeLiftSpeed.X = platform.LiftSpeed.X;
                dynamicData.Set("safeLiftSpeed", safeLiftSpeed);
            });

            cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Stloc_0);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.EmitDelegate<Action<Platform, int>>((platform, move) => {
                if (!GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)
                    || move != 0 || platform.LiftSpeed.X == 0f || !platform.Collidable || platform is not Solid solid)
                    return;

                foreach (var entity in platform.Scene.Tracker.GetEntities<Actor>()) {
                    var actor = (Actor) entity;

                    if (actor.IsRiding(solid))
                        actor.LiftSpeed = solid.LiftSpeed;
                }
            });
        }

        private void PatchLiftboostProtectionY(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.Index = -1;
            cursor.GotoPrev(instr => instr.MatchLdflda<Platform>("LiftSpeed"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchStfld<Vector2>("Y"));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Platform>>(platform => {
                if (!GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection))
                    return;

                float liftSpeed = platform.LiftSpeed.Y;

                if (liftSpeed == 0f)
                    return;

                var dynamicData = DynamicData.For(platform);
                float minusTwo = dynamicData.Get<Vector2?>("safeLiftSpeedMinusTwo")?.Y ?? 0f;
                float minusOne = dynamicData.Get<Vector2?>("safeLiftSpeedMinusOne")?.Y ?? 0f;

                platform.LiftSpeed.Y = CorrectLiftSpeed(minusTwo, minusOne, liftSpeed);

                var safeLiftSpeed = dynamicData.Get<Vector2?>("safeLiftSpeed") ?? Vector2.Zero;

                safeLiftSpeed.Y = platform.LiftSpeed.Y;
                dynamicData.Set("safeLiftSpeed", safeLiftSpeed);
            });

            cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Stloc_0);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.EmitDelegate<Action<Platform, int>>((platform, move) => {
                if (!GetVariantValue<bool>(ExtendedVariantsModule.Variant.LiftboostProtection)
                    || move != 0 || platform.LiftSpeed.Y == 0f || !platform.Collidable)
                    return;

                if (platform is Solid solid) {
                    foreach (var entity in platform.Scene.Tracker.GetEntities<Actor>()) {
                        var actor = (Actor) entity;

                        if (actor.IsRiding(solid))
                            actor.LiftSpeed = solid.LiftSpeed;
                    }
                }
                else if (platform is JumpThru jumpThru) {
                    foreach (var entity in platform.Scene.Tracker.GetEntities<Actor>()) {
                        var actor = (Actor) entity;

                        if (actor.IsRiding(jumpThru))
                            actor.LiftSpeed = jumpThru.LiftSpeed;
                    }
                }
            });
        }
    }
}
