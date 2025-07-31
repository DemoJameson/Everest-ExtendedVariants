﻿using Celeste;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Collections;
using MonoMod.RuntimeDetour;
using System;
using Celeste.Mod.EV;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class DashDirection : AbstractExtendedVariant {

        private static FieldInfo playerLastAim = typeof(Player).GetField("lastAim", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo playerCalledDashEvents = typeof(Player).GetField("calledDashEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo playerBeforeDashSpeed = typeof(Player).GetField("beforeDashSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo playerLastDashes = typeof(Player).GetField("lastDashes", BindingFlags.NonPublic | BindingFlags.Instance);

        private Hook canDashHook;
        private int dashCountBeforeDash;
        private Vector2 dashDirectionBeforeDash;

        public DashDirection() : base(variantType: typeof(bool[][]), defaultVariantValue: new bool[][] { new bool[] { true, true, true }, new bool[] { true, true, true }, new bool[] { true, true, true } }) { }

        public override object ConvertLegacyVariantValue(int value) {
            // how the H did people deal with bit math in extended variant triggers???
            int TOP = 0b1000000000;
            int TOP_RIGHT = 0b0100000000;
            int RIGHT = 0b0010000000;
            int BOTTOM_RIGHT = 0b0001000000;
            int BOTTOM = 0b0000100000;
            int BOTTOM_LEFT = 0b0000010000;
            int LEFT = 0b0000001000;
            int TOP_LEFT = 0b0000000100;

            if (value == 0) {
                // all directions allowed
                value = 0b1111111111;
            } else if (value == 1) {
                // straight only
                value = 0b1010101000;
            } else if (value == 2) {
                // diagonal only
                value = 0b0101010100;
            }

            return new bool[][] {
                new bool[] { (value & TOP_LEFT) != 0, (value & TOP) != 0, (value & TOP_RIGHT) != 0 },
                new bool[] { (value & LEFT) != 0, true, (value & RIGHT) != 0 },
                new bool[] { (value & BOTTOM_LEFT) != 0, (value & BOTTOM) != 0, (value & BOTTOM_RIGHT) != 0 },
            };
        }

        public override void Load() {
            canDashHook = new Hook(typeof(Player).GetMethod("get_CanDash"), typeof(DashDirection).GetMethod("modCanDash", BindingFlags.NonPublic | BindingFlags.Instance), this);
            On.Celeste.Player.StartDash += onStartDash;
            On.Celeste.Player.BoostEnd += onBoostEnd;
            On.Celeste.Player.DashCoroutine += onDashCoroutine;
            On.Celeste.Player.RedDashCoroutine += onRedDashCoroutine;
        }

        public override void Unload() {
            canDashHook?.Dispose();
            On.Celeste.Player.StartDash -= onStartDash;
            On.Celeste.Player.BoostEnd -= onBoostEnd;
            On.Celeste.Player.DashCoroutine -= onDashCoroutine;
            On.Celeste.Player.RedDashCoroutine -= onRedDashCoroutine;
        }

        private delegate bool orig_CanDash(Player self);

        private bool modCanDash(orig_CanDash orig, Player self) {
            Vector2 aim = Input.GetAimVector();

            // block the dash directly if the player is holding a forbidden direction, and does not have Dash Assist enabled.
            return orig(self) && (SaveData.Instance.Assists.DashAssist || isDashDirectionAllowed(aim));
        }

        private int onStartDash(On.Celeste.Player.orig_StartDash orig, Player self) {
            dashCountBeforeDash = self.Dashes;
            dashDirectionBeforeDash = (Vector2) playerLastAim.GetValue(self);
            return orig(self);
        }

        private void onBoostEnd(On.Celeste.Player.orig_BoostEnd orig, Player self) {
            if (self.StateMachine.State == Player.StDash || self.StateMachine.State == Player.StRedDash) {
                dashCountBeforeDash = self.Dashes;
                dashDirectionBeforeDash = (Vector2) playerLastAim.GetValue(self);
            }

            orig(self);
        }

        private IEnumerator onDashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player self) {
            if (areAllDirectionsAllowed()) {
                return orig(self);
            }
            return modDashCoroutine(orig(self), self);
        }

        private IEnumerator onRedDashCoroutine(On.Celeste.Player.orig_RedDashCoroutine orig, Player self) {
            if (areAllDirectionsAllowed()) {
                return orig(self);
            }
            return modDashCoroutine(orig(self), self);
        }

        private IEnumerator modDashCoroutine(IEnumerator vanillaCoroutine, Player self) {
            // make a step forward
            vanillaCoroutine = vanillaCoroutine.SafeEnumerate();
            if (vanillaCoroutine.MoveNext()) {
                yield return vanillaCoroutine.Current;
            }

            // get the dash general direction
            Vector2 direction;
            if (self.OverrideDashDirection.HasValue) {
                direction = self.OverrideDashDirection.Value;
            } else {
                direction = (Vector2) playerLastAim.GetValue(self);
            }

            if (isDashDirectionAllowed(direction)) {
                // continue with the dash like normal.
                while (vanillaCoroutine.MoveNext()) {
                    yield return vanillaCoroutine.Current;
                }

                yield break;
            }

            // forbidden direction! aaa
            if (direction != dashDirectionBeforeDash && isDashDirectionAllowed(dashDirectionBeforeDash)) {
                // an allowed dash direction was held before the freeze frames, so just redirect the dash
                playerLastAim.SetValue(self, dashDirectionBeforeDash);

                // continue with the dash like normal.
                while (vanillaCoroutine.MoveNext()) {
                    yield return vanillaCoroutine.Current;
                }

                yield break;
            }

            // prevent DashEnd from triggering dash events (no dash sound)
            playerCalledDashEvents.SetValue(self, true);
            // restore pre-dash speed
            self.Speed = (Vector2) playerBeforeDashSpeed.GetValue(self);
            // restore pre-dash dash count
            self.Dashes = dashCountBeforeDash;
            // prevent the hair from flashing
            playerLastDashes.SetValue(self, self.Dashes);

            // if in a bubble, make the bubble explode
            if (self.CurrentBooster != null) {
                self.CurrentBooster.PlayerReleased();
                self.CurrentBooster = null;
            }

            // kick the player back to the normal state
            self.StateMachine.State = 0;
        }

        private bool areAllDirectionsAllowed() {
            foreach (bool[] ba in GetVariantValue<bool[][]>(Variant.DashDirection)) {
                foreach (bool b in ba) {
                    if (!b) return false;
                }
            }
            return true;
        }

        private bool isDashDirectionAllowed(Vector2 direction) {
            // if directions are not integers, make them integers.
            direction = new Vector2(Math.Sign(direction.X), Math.Sign(direction.Y));

            // bottom-left (-1, 1) is row 2, column 0.
            return GetVariantValue<bool[][]>(Variant.DashDirection)[(int) (direction.Y + 1)][(int) (direction.X + 1)];
        }
    }
}
