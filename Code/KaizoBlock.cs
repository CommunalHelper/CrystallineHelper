using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace vitmod
{
    [CustomEntity("vitellary/kaizoblock")]
    [Tracked]
    public class KaizoBlock : Solid
    {
        public static void Load()
        {
            On.Celeste.Player.Update += Player_Update;
        }

        public static void Unload()
        {
            On.Celeste.Player.Update -= Player_Update;
        }

        public KaizoBlock(EntityData data, Vector2 offset, EntityID ID) : base(data.Position + offset - new Vector2(8, 8), 16, 16, true)
        {
            persistent = data.Bool("persistent", false);
            dashDisable = data.Float("noDashTimer", 0.2f);

            Depth = -9999;
            id = ID.ToString();
            Collidable = false;

            Add(sprite = GFX.SpriteBank.Create("kaizoblock"));
            sprite.Play("idle");
            sprite.Visible = false;
            sprite.Position = new Vector2(8, 8);
            Add(spriteWiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));
            moveWiggler = Wiggler.Create(0.8f, 2f);
            moveWiggler.StartZero = true;
            Add(moveWiggler);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Level level = scene as Level;
            if (persistent && level.Session.GetFlag("kaizoblock_flag_" + id))
            {
                activated = true;
                Collidable = true;
                sprite.Visible = true;
            }
            scene.Add(new OutlineRenderer(this));
        }

        public override void Update()
        {
            base.Update();
            if (!activated)
            {
                Player player = SceneAs<Level>().Tracker.GetEntity<Player>();
                if (player == null) return;
                if (CollideCheck<Player>(Position + Vector2.UnitY) && player.Speed.Y < 0f)
                {
                    Hit(player);
                }
            }
        }

        private void Hit(Player player)
        {
            player.Speed.Y = 160f;
            player.StateMachine.State = 0;
            player.NaiveMove(new Vector2(0, Bottom - player.Top));
            DynData<Player> playerData = new DynData<Player>(player);
            playerData.Set("dashCooldownTimer", dashDisable);
            
            Audio.Play("event:/game/general/thing_booped", Position);
            sprite.Play("flash");
            sprite.Visible = true;
            Add(Alarm.Create(Alarm.AlarmMode.Oneshot, delegate
            {
                Audio.Play("event:/game/07_summit/checkpoint_confetti", Position);
            }, 0.25f, start: true));
            spriteWiggler.Start();
            moveWiggler.Start();
            activated = true;
            Collidable = true;

            if (persistent)
            {
                SceneAs<Level>().Session.SetFlag("kaizoblock_flag_" + id);
            }
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            List<Entity> kaizoBlocks = self.SceneAs<Level>().Tracker.GetEntities<KaizoBlock>();
            kaizoBlocks = kaizoBlocks.Where(kaizo => !(kaizo as KaizoBlock).activated).ToList();
            foreach (KaizoBlock kaizo in kaizoBlocks)
            {
                kaizo.Collidable = kaizo.Bottom < self.Top;
            }
            orig(self);
            foreach (KaizoBlock kaizo in kaizoBlocks)
            {
                kaizo.Collidable = false;
            }
        }

        private class OutlineRenderer : Entity
        {
            public KaizoBlock kaizo;

            public OutlineRenderer(KaizoBlock kaizoBlock)
            {
                Depth = 5000;
                kaizo = kaizoBlock;
            }

            public override void Render()
            {
                if (kaizo.activated)
                {
                    Draw.Rect(new Rectangle((int)(kaizo.X + kaizo.Shake.X), (int)(kaizo.Y + kaizo.Shake.Y - 1f), (int)kaizo.Width, (int)kaizo.Height + 2), Color.Black);
                    Draw.Rect(new Rectangle((int)(kaizo.X + kaizo.Shake.X - 1f), (int)(kaizo.Y + kaizo.Shake.Y), (int)kaizo.Width + 2, (int)kaizo.Height), Color.Black);
                }
            }
        }

        private bool persistent;
        private float dashDisable;
        private string id;

        public bool activated;
        private Sprite sprite;
        private Wiggler spriteWiggler;
        private Wiggler moveWiggler;
    }
}
