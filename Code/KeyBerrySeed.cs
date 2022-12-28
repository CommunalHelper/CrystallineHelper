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
    [Tracked]
    public class KeyBerrySeed : Entity
    {
        public KeyBerrySeed(KeyBerry keyberry, Vector2 position, int index) : base(position)
        {
            this.keyberry = keyberry;
            Depth = -100;
            start = Position;
            Collider = new Hitbox(12f, 12f, -6f, -6f);
            this.index = index;
            Add(follower = new Follower(new Action(OnGainLeader), new Action(OnLoseLeader)));
            follower.FollowDelay = 0.2f;
            follower.PersistentFollow = false;
            Add(new StaticMover
            {
                SolidChecker = ((Solid s) => s.CollideCheck(this)),
                OnAttach = delegate (Platform p)
                {
                    Depth = -1000000;
                    Collider = new Hitbox(24f, 24f, -12f, -12f);
                    attached = p;
                    start = Position - p.Position;
                }
            });
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float v)
            {
                sprite.Scale = Vector2.One * (1f + 0.2f * v);
            }, false, false));
            Add(sine = new SineWave(0.5f, 0f).Randomize());
            Add(shaker = new Shaker(false, null));
            Add(bloom = new BloomPoint(1f, 12f));
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(lightTween = light.CreatePulseTween());
        }

        public override void Awake(Scene scene)
        {
            level = SceneAs<Level>();
            base.Awake(scene);
            sprite = GFX.SpriteBank.Create("keyberrySeed");
            sprite.Position = new Vector2(sine.Value * 2f, sine.ValueOverTwo * 1f);
            Add(sprite);
            float spriteoffset = 1f - index / (Scene.Tracker.CountEntities<KeyBerrySeed>() + 1f);
            spriteoffset = 0.25f + spriteoffset * 0.75f;
            sprite.PlayOffset("idle", spriteoffset, false);
            sprite.OnFrameChange = delegate (string s)
            {
                if (Visible && sprite.CurrentAnimationID == "idle" && sprite.CurrentAnimationFrame == 19)
                {
                    Audio.Play("event:/game/general/seed_pulse", Position, "count", index);
                    lightTween.Start();
                    level.Displacement.AddBurst(Position, 0.6f, 8f, 20f, 0.2f, null, null);
                }
            };
        }

        public override void Update()
        {
            base.Update();
            if (!finished)
            {
                if (canLoseTimer > 0f)
                {
                    canLoseTimer -= Engine.DeltaTime;
                }
                else
                {
                    if (follower.HasLeader && player.LoseShards)
                    {
                        losing = true;
                    }
                }
                if (losing)
                {
                    if (loseTimer <= 0f || player.Speed.Y < 0f)
                    {
                        player.Leader.LoseFollower(follower);
                        losing = false;
                    }
                    else
                    {
                        if (player.LoseShards)
                        {
                            loseTimer -= Engine.DeltaTime;
                        }
                        else
                        {
                            loseTimer = 0.15f;
                            losing = false;
                        }
                    }
                }
                sprite.Position = new Vector2(sine.Value * 2f, sine.ValueOverTwo * 1f) + shaker.Value;
            }
            else
            {
                light.Alpha = Calc.Approach(light.Alpha, 0f, Engine.DeltaTime * 4f);
            }
        }

        private void OnPlayer(Player player)
        {
            Audio.Play("event:/game/general/seed_touch", Position, "count", index);
            this.player = player;
            player.Leader.GainFollower(follower);
            Collidable = false;
            Depth = -1000000;
            bool complete = true;
            foreach (KeyBerrySeed keyberrySeed in keyberry.Seeds)
            {
                if (!keyberrySeed.follower.HasLeader)
                {
                    complete = false;
                }
            }
            if (complete)
            {
                Add(new Coroutine(CutsceneRoutine(), true));
            }
        }

        private void OnGainLeader()
        {
            collected = true;
            wiggler.Start();
            canLoseTimer = 0.25f;
            loseTimer = 0.15f;
        }

        private void OnLoseLeader()
        {
            collected = false;
            if (!finished)
            {
                Add(new Coroutine(ReturnRoutine(), true));
            }
        }

        private IEnumerator CutsceneRoutine()
        {
            Level level = SceneAs<Level>();
            foreach (KeyBerrySeed seed in keyberry.Seeds)
            {
                seed.OnAllCollected();
            }
            keyberry.Depth = -2000002;
            keyberry.AddTag(Tags.FrozenUpdate);
            yield return 0.35f;
            ParticleSystem system = new ParticleSystem(-2000002, 50);
            system.Tag = Tags.FrozenUpdate;
            level.Add(system);
            float angleSep = 6.28318548f / keyberry.Seeds.Count;
            float angle = 1.57079637f;
            Vector2 avg = Vector2.Zero;
            foreach (KeyBerrySeed seed in keyberry.Seeds)
            {
                avg += seed.Position;
            }
            avg /= keyberry.Seeds.Count;
            foreach (KeyBerrySeed seed in keyberry.Seeds)
            {
                seed.StartSpinAnimation(avg, keyberry.Position, angle, 1f);
                angle -= angleSep;
            }
            avg = default;
            yield return 0.9f;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            Audio.Play("event:/game/general/seed_complete_berry", keyberry.Position);
            foreach (KeyBerrySeed seed in keyberry.Seeds)
            {
                seed.StartCombineAnimation(keyberry.Position, 0.3f, system);
            }
            yield return 0.3f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            foreach (KeyBerrySeed seed in keyberry.Seeds)
            {
                seed.RemoveSelf();
            }
            keyberry.CollectedSeeds();
            yield return 0.5f;
            keyberry.Depth = -100;
            keyberry.RemoveTag(Tags.FrozenUpdate);
            yield break;
        }

        private IEnumerator ReturnRoutine()
        {
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 2f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            int num;
            for (int i = 0; i < 6; i = num + 1)
            {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                num = i;
            }
            Visible = false;
            yield return 0.3f + index * 0.1f;
            Audio.Play("event:/game/general/seed_reappear", Position, "count", index);
            Position = start;
            if (attached != null)
            {
                Position += attached.Position;
            }
            shaker.ShakeFor(0.4f, false);
            sprite.Scale = Vector2.One;
            Visible = true;
            Collidable = true;
            level.Displacement.AddBurst(Position, 0.2f, 8f, 28f, 0.2f, null, null);
            yield break;
        }

        public void OnAllCollected()
        {
            finished = true;
            follower.Leader.LoseFollower(follower);
            Depth = -2000002;
            Tag = Tags.FrozenUpdate;
            wiggler.Start();
        }

        public void StartSpinAnimation(Vector2 averagePos, Vector2 centerPos, float angleOffset, float time)
        {
            float spinLerp = 0f;
            Vector2 start = Position;
            sprite.Play("noFlash", false, false);
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, time / 2f, true);
            tween.OnUpdate = delegate (Tween t)
            {
                spinLerp = t.Eased;
            };
            Add(tween);
            tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, time, true);
            tween.OnUpdate = delegate (Tween t)
            {
                float angleRadians = 1.57079637f + angleOffset - MathHelper.Lerp(0f, 15.7079633f, t.Eased);
                Vector2 value = Vector2.Lerp(averagePos, centerPos, spinLerp);
                Vector2 value2 = value + Calc.AngleToVector(angleRadians, 25f);
                Position = Vector2.Lerp(start, value2, spinLerp);
            };
            Add(tween);
        }

        public void StartCombineAnimation(Vector2 centerPos, float time, ParticleSystem particleSystem)
        {
            Vector2 position = Position;
            float startAngle = Calc.Angle(centerPos, position);
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BigBackIn, time, true);
            tween.OnUpdate = delegate (Tween t)
            {
                float angleRadians = MathHelper.Lerp(startAngle, startAngle - 6.28318548f, Ease.CubeIn(t.Percent));
                float length = MathHelper.Lerp(25f, 0f, t.Eased);
                Position = centerPos + Calc.AngleToVector(angleRadians, length);
            };
            tween.OnComplete = delegate (Tween t)
            {
                Visible = false;
                for (int i = 0; i < 6; i++)
                {
                    float num = Calc.Random.NextFloat(6.28318548f);
                    particleSystem.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(num, 4f), Vector2.Zero, num);
                }
            };
            Add(tween);
        }

        private Platform attached;

        private BloomPoint bloom;

        private float canLoseTimer;

        private bool finished;

        private Follower follower;

        private int index;

        private Level level;

        private VertexLight light;

        private Tween lightTween;

        private const float LoseDelay = 0.25f;

        private const float LoseGraceTime = 0.15f;

        private float loseTimer;

        private bool losing;

        public bool collected;

        private Player player;

        private Shaker shaker;

        private SineWave sine;

        private Sprite sprite;

        private Vector2 start;

        public KeyBerry keyberry;

        private Wiggler wiggler;
    }
}
