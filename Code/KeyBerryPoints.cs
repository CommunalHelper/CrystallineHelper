using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vitmod
{
    [TrackedAs(typeof(StrawberryPoints))]
    public class KeyBerryPoints : Entity
    {
        public KeyBerryPoints(Vector2 position, int index) : base(position)
        {
            Add(sprite = GFX.SpriteBank.Create("keyberry"));
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(bloom = new BloomPoint(1f, 12f));
            Depth = -2000100;
            Tag = (Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate);
            this.index = index;
        }

        public override void Added(Scene scene)
        {
            if (index == 5)
            {
                sprite.Play("fade1up", false, false);
            }
            else
            {
                sprite.Play("fade", false, false);
            }
            sprite.OnFinish = delegate (string a)
            {
                RemoveSelf();
            };
            base.Added(scene);
            foreach (Entity entity in Scene.Tracker.GetEntities<StrawberryPoints>())
            {
                if (entity != this && Vector2.DistanceSquared(entity.Position, Position) <= 256f)
                {
                    entity.RemoveSelf();
                }
            }
        }

        public override void Update()
        {
            Level level = SceneAs<Level>();
            if (level.Frozen)
            {
                if (burst != null)
                {
                    burst.AlphaFrom = (burst.AlphaTo = 0f);
                    burst.Percent = burst.Duration;
                }
            }
            else
            {
                base.Update();
                Camera camera = level.Camera;
                Y -= 8f * Engine.DeltaTime;
                X = Calc.Clamp(X, camera.Left + 8f, camera.Right - 8f);
                Y = Calc.Clamp(Y, camera.Top + 8f, camera.Bottom - 8f);
                light.Alpha = Calc.Approach(light.Alpha, 0f, Engine.DeltaTime * 4f);
                bloom.Alpha = light.Alpha;
                if (Scene.OnInterval(0.05f))
                {
                    if (sprite.Color == Strawberry.P_Glow.Color)
                    {
                        sprite.Color = Strawberry.P_Glow.Color2;
                    }
                    else
                    {
                        sprite.Color = Strawberry.P_Glow.Color;
                    }
                }
                if (Scene.OnInterval(0.06f) && sprite.CurrentAnimationFrame > 11)
                {
                    level.ParticlesFG.Emit(Strawberry.P_Glow, 1, Position + Vector2.UnitY * -2f, new Vector2(8f, 4f));
                }
            }
        }

        private Sprite sprite;

        private BloomPoint bloom;

        private DisplacementRenderer.Burst burst;

        private int index;

        private VertexLight light;
    }
}
