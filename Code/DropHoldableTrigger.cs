using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vitmod {
    [CustomEntity("vitellary/dropholdables")]
    public class DropHoldableTrigger : Celeste.Trigger {
        private static bool pickupDisabled = false;

        public DropHoldableTrigger(Celeste.EntityData data, Vector2 offset) : base(data, offset) {


        }
        public static void Load() {
            On.Celeste.Player.Pickup += pickupOverride;
        }

        public static void Unload() {
            On.Celeste.Player.Pickup -= pickupOverride;
        }

        private static bool pickupOverride(On.Celeste.Player.orig_Pickup orig, Player player, Holdable hold) {
            if (pickupDisabled) {
                return false;
            }
            else {
                return orig(player, hold);
            }
        }
        public override void OnEnter(Player player) {
            base.OnEnter(player);
            player.Drop();
            pickupDisabled = true;
        }

        public override void OnLeave(Player player) {
            pickupDisabled = false;

        }
    }
 }

