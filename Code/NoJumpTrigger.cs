using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vitmod
{
    [CustomEntity("vitellary/nojumptrigger")]
    [Tracked(false)]
    public class NoJumpTrigger : Trigger
    {
        public static void Load()
        {
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.SuperJump += Player_SuperJump;
        }

        public static void Unload()
        {
            On.Celeste.Player.Jump -= Player_Jump;
            On.Celeste.Player.SuperJump -= Player_SuperJump;
        }

        public NoJumpTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        public static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
        {
            grabbing = self.CollideCheck<Solid>(self.Position + Vector2.UnitX * (float) self.Facing) && Input.Grab.Check;
            if (!self.CollideCheck<NoJumpTrigger>() || grabbing)
            {
                orig(self, particles, playSfx);
            }
        }

        public static void Player_SuperJump(On.Celeste.Player.orig_SuperJump orig, Player self)
        {
            if (!self.CollideCheck<NoJumpTrigger>())
            {
                orig(self);
            }
        }

        private static bool grabbing;
    }
}
