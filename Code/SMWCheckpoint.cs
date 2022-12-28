using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace vitmod
{
    [CustomEntity("vitellary/smwcheckpoint")]
    public class SMWCheckpoint : Entity
    {
        private Image checkpoint;
        private Vector2 respawn;
        private float poleHeight;

        public SMWCheckpoint(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            float barHeight = data.Nodes[0].Y - data.Position.Y;
            poleHeight = data.Height;
            if (data.Bool("fullHeight"))
            {
                Collider = new Hitbox(12, poleHeight, 2, 0);
            }
            else
            {
                Collider = new Hitbox(12, 4, 2, barHeight);
            }
            Add(new PlayerCollider(OnPlayer));
            Depth = 1;
            checkpoint = new Image(GFX.Game["objects/smwCheckpoint/cp"]);
            checkpoint.Position = new Vector2(2, barHeight);
            Add(checkpoint);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(new BarRenderer(Position, false, poleHeight));
            scene.Add(new BarRenderer(Position + new Vector2(12, 0), true, poleHeight));
            Level level = scene as Level;
            respawn = level.GetSpawnPoint(BottomCenter);
            if (respawn == level.Session.RespawnPoint)
            {
                Collidable = false;
                Remove(checkpoint);
            }
        }

        private void OnPlayer(Player player)
        {
            Collidable = false;
            Remove(checkpoint);
            Level level = Scene as Level;
            level.Session.RespawnPoint = new Vector2?(respawn);
            level.Session.UpdateLevelStartDashes();
            level.Session.HitCheckpoint = true;
            Audio.Play("event:/game/07_summit/checkpoint_confetti", Center);
        }

        private class BarRenderer : Entity
        {
            public BarRenderer(Vector2 position, bool fg, float height) : base(position)
            {
                MTexture bars = GFX.Game["objects/smwCheckpoint/bars"];
                Depth = fg ? -9000 : 100;
                for (int i = 0; i < height; i += 8)
                {
                    Image barTexture;
                    if (i == 0)
                    {
                        barTexture = new Image(bars.GetSubtexture(fg ? 4 : 0, 0, 4, 8));
                    }
                    else
                    {
                        barTexture = new Image(bars.GetSubtexture(fg ? 4 : 0, 8, 4, 8));
                    }
                    barTexture.Position.Y = i;
                    Add(barTexture);
                }
            }
        }
    }
}
