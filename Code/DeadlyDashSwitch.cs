using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace vitmod
{
    [CustomEntity("vitellary/deadlydashswitch")]
    public class DeadlyDashSwitch : DashSwitch
    {
        public DeadlyDashSwitch(EntityData data, Vector2 offset) : base(data.Position + offset, sideDict[data.Attr("direction", "Left")], data.Bool("persistent"), false, new EntityID(data.Level.Name, data.ID), "deadly")
        {
            direction = sideDict[data.Attr("direction", "Left")];
            switch (direction)
            {
                case Sides.Left:
                case Sides.Right:
                    Add(new PlayerCollider(OnPlayerCollide, new Hitbox(10f, 20f, -1f, -3f)));
                    break;
                case Sides.Up:
                case Sides.Down:
                    Add(new PlayerCollider(OnPlayerCollide, new Hitbox(18f, 12f, -1f, -3f)));
                    break;
            }
        }

        public static void Load()
        {
            On.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
        }

        public static void Unload()
        {
            On.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
        }

        private static DashCollisionResults DashSwitch_OnDashed(On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction)
        {
            DashCollisionResults result = orig(self, player, direction);
            if (self is DeadlyDashSwitch)
            {
                DynData<DashSwitch> switchData = new DynData<DashSwitch>(self);
                if (!switchData.Get<bool>("pressed"))
                {
                    player.Die(Vector2.Clamp(player.Center - self.Center, -Vector2.One, Vector2.One));
                }
                return DashCollisionResults.Ignore;
            }
            return result;
        }

        private void OnPlayerCollide(Player player)
        {
            if (!player.DashAttacking)
            {
                player.Die(Vector2.Clamp(player.Center - Center, -Vector2.One, Vector2.One));
            }
            else
            {
                switch (direction)
                {
                    case Sides.Left:
                        if (player.Speed.X >= 0f || player.Bottom < Top)
                        {
                            player.Die(Vector2.Clamp(player.Center - Center, -Vector2.One, Vector2.One));
                        }
                        break;
                    case Sides.Up:
                        if (player.Speed.Y >= 0f)
                        {
                            player.Die(Vector2.Clamp(player.Center - Center, -Vector2.One, Vector2.One));
                        }
                        break;
                    case Sides.Right:
                        if (player.Speed.X <= 0f || player.Bottom < Top)
                        {
                            player.Die(Vector2.Clamp(player.Center - Center, -Vector2.One, Vector2.One));
                        }
                        break;
                    case Sides.Down:
                        if (player.Speed.Y <= 0f)
                        {
                            player.Die(Vector2.Clamp(player.Center - Center, -Vector2.One, Vector2.One));
                        }
                        break;
                }
            }
        }

        private Sides direction;

        private static Dictionary<string, Sides> sideDict = new Dictionary<string, Sides>()
        {
            { "Left", Sides.Right },
            { "Up", Sides.Down },
            { "Right", Sides.Left },
            { "Down", Sides.Up }
        };
    }
}
