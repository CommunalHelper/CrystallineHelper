using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
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
    [CustomEntity("vitellary/keyberry", "vitellary/returnkeyberry")]
    [TrackedAs(typeof(Strawberry))]
    public class KeyBerry : Entity
    {
        public KeyBerry(EntityData data, Vector2 offset, EntityID gid) : base(data.Position + offset)
        {
            wobble = 0f;
            collectTimer = 0f;
            collected = false;
            returnHomeWhenLost = true;
            ID = gid;
            winged = data.Bool("winged", false);
            hasReturnBubble = data.Name == "vitellary/returnkeyberry";
            start = Position;
            Depth = -100;
            Collider = new Hitbox(14f, 14f, -7f, -7f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(new MirrorReflection());
            Add(Follower = new Follower(ID, null, new Action(OnLoseLeader)));
            Follower.FollowDelay = 0.3f;
            if (winged)
            {
                Add(new DashListener
                {
                    OnDash = new Action<Vector2>(OnDash)
                });
            }
            if (data.Nodes != null && data.Nodes.Length != 0)
            {
                int node_max = data.Nodes.Length;
                if (hasReturnBubble)
                {
                    node_max = data.Nodes.Length - 2;
                    bubbleControl = data.Nodes[node_max] + offset;
                    bubbleEnd = data.Nodes[node_max + 1] + offset;
                }
                Seeds = new List<KeyBerrySeed>();
                for (int i = 0; i < node_max; i++)
                {
                    Seeds.Add(new KeyBerrySeed(this, offset + data.Nodes[i], i));
                }
            }
            if (!VitModule.Session.keyberryEntityDatas.ContainsKey(gid))
            {
                VitModule.Session.keyberryEntityDatas.Add(gid, data);
                VitModule.Session.keyberryPositions.Add(gid, data.Position);
                VitModule.Session.keyberryOffsets.Add(gid, offset);
                VitModule.Session.keyberryBubbles.Add(gid, hasReturnBubble);
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            sprite = GFX.SpriteBank.Create("keyberry");
            Add(sprite);
            if (winged)
            {
                sprite.Play("flap", false, false);
            }
            sprite.OnFrameChange = new Action<string>(OnAnimate);
            Add(wiggler = Wiggler.Create(0.4f, 4f, delegate (float v)
            {
                sprite.Scale = Vector2.One * (1f + v * 0.35f);
            }, false, false));
            Add(rotateWiggler = Wiggler.Create(0.5f, 4f, delegate (float v)
            {
                sprite.Rotation = v * 30f * 0.0174532924f;
            }, false, false));
            Add(bloom = new BloomPoint(1f, 12f));
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(lightTween = light.CreatePulseTween());
            if (Seeds != null && Seeds.Count > 0)
            {
                foreach (KeyBerrySeed entity in Seeds)
                {
                    scene.Add(entity);
                }
                Visible = false;
                Collidable = false;
                waitingOnSeeds = true;
                bloom.Visible = (light.Visible = false);
            }
            if (SceneAs<Level>().Session.BloomBaseAdd > 0.1f)
            {
                bloom.Alpha *= 0.5f;
            }
        }

        public override void Update()
        {
            if (!waitingOnSeeds)
            {
                if (!collected)
                {
                    if (!winged)
                    {
                        wobble += Engine.DeltaTime * 4f;
                        sprite.Y = (bloom.Y = (light.Y = (float)Math.Sin(wobble) * 2f));
                    }
                    int followIndex = Follower.FollowIndex;
                    bool islastberry = false;
                    for (int i = followIndex - 1; i >= 0; i--)
                    {
                        Entity entity = Follower.Leader.Followers[i].Entity;
                        if (entity is KeyBerry)
                        {
                            islastberry = true;
                            break;
                        }
                        if (Celeste.Mod.StrawberryRegistry.IsFirstStrawberry(entity))
                        {
                            break;
                        }
                    }
                    if (Follower.Leader != null && Follower.DelayTimer <= 0f && !islastberry)
                    {
                        Player player = Follower.Leader.Entity as Player;
                        if (player != null && player.Scene != null && !player.StrawberriesBlocked)
                        {
                            if (player.OnSafeGround)
                            {
                                collectTimer += Engine.DeltaTime;
                                if (collectTimer > 0.15f)
                                {
                                    OnCollect();
                                }
                            }
                            else
                            {
                                collectTimer = Math.Min(collectTimer, 0f);
                            }
                        }
                    }
                    else
                    {
                        if (followIndex > 0)
                        {
                            collectTimer = -0.15f;
                        }
                        if (winged)
                        {
                            Y += flapSpeed * Engine.DeltaTime;
                            if (flyingAway)
                            {
                                if (Y < SceneAs<Level>().Bounds.Top - 16)
                                {
                                    RemoveSelf();
                                }
                            }
                            else
                            {
                                flapSpeed = Calc.Approach(flapSpeed, 20f, 170f * Engine.DeltaTime);
                                if (Y < start.Y - 5f)
                                {
                                    Y = start.Y - 5f;
                                }
                                else if (Y > start.Y + 5f)
                                {
                                    Y = start.Y + 5f;
                                }
                            }
                        }
                    }
                }
                base.Update();
                if (Follower.Leader != null && Scene.OnInterval(0.08f))
                {
                    SceneAs<Level>().ParticlesFG.Emit(Strawberry.P_GoldGlow, Position + Calc.Random.Range(-Vector2.One * 6f, Vector2.One * 6f));
                }
            }
        }

        private void OnPlayer(Player player)
        {
            if (Follower.Leader == null && !collected && !waitingOnSeeds)
            {
                returnHomeWhenLost = true;
                if (winged)
                {
                    Level level = SceneAs<Level>();
                    winged = false;
                    sprite.Rate = 0f;
                    Alarm.Set(this, Follower.FollowDelay, delegate
                    {
                        sprite.Rate = 1f;
                        sprite.Play("idle", false, false);
                        level.Particles.Emit(Strawberry.P_WingsBurst, 8, Position + new Vector2(8f, 0f), new Vector2(4f, 2f));
                        level.Particles.Emit(Strawberry.P_WingsBurst, 8, Position - new Vector2(8f, 0f), new Vector2(4f, 2f));
                    }, Alarm.AlarmMode.Oneshot);
                }
                Audio.Play("event:/game/general/key_get", Position);
                // player.Leader.GainFollower(Follower);
                player.Leader.Followers.Insert(0, Follower);
                Follower.OnGainLeaderUtil(player.Leader);
                wiggler.Start();
                Depth = -1000000;
                if (hasReturnBubble)
                {
                    Add(new Coroutine(ReturnRoutine(player)));
                }
            }
        }

        private IEnumerator ReturnRoutine(Player player)
        {
            yield return 0.3f;
            bool flag = !player.Dead;
            if (flag)
            {
                Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
                player.StartCassetteFly(bubbleEnd, bubbleControl);
            }
            yield break;
        }

        private void OnLoseLeader()
        {
            if (!collected && returnHomeWhenLost)
            {
                Alarm.Set(this, 0.15f, delegate
                {
                    Vector2 vector = (start - Position).SafeNormalize();
                    float num = Vector2.Distance(Position, start);
                    float scaleFactor = Calc.ClampedMap(num, 16f, 120f, 16f, 96f);
                    Vector2 control = start + vector * 16f + vector.Perpendicular() * scaleFactor * Calc.Random.Choose(1, -1);
                    SimpleCurve curve = new SimpleCurve(Position, start, control);
                    Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineOut, MathHelper.Max(num / 100f, 0.4f), true);
                    tween.OnUpdate = delegate (Tween f)
                    {
                        Position = curve.GetPoint(f.Eased);
                    };
                    tween.OnComplete = delegate (Tween f)
                    {
                        Depth = 0;
                    };
                    Add(tween);
                }, Alarm.AlarmMode.Oneshot);
            }
        }

        private void OnDash(Vector2 dir)
        {
            if (!flyingAway && winged && !waitingOnSeeds)
            {
                Depth = -1000000;
                Add(new Coroutine(FlyAwayRoutine(), true));
                flyingAway = true;
            }
        }

        private IEnumerator FlyAwayRoutine()
        {
            rotateWiggler.Start();
            flapSpeed = -200f;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 0.25f, true);
            tween.OnUpdate = delegate (Tween t)
            {
                flapSpeed = MathHelper.Lerp(-200f, 0f, t.Eased);
            };
            Add(tween);
            yield return 0.1f;
            Audio.Play("event:/game/general/strawberry_laugh", Position);
            yield return 0.2f;
            if (!Follower.HasLeader)
            {
                Audio.Play("event:/game/general/strawberry_flyaway", Position);
            }
            tween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.5f, true);
            tween.OnUpdate = delegate (Tween t)
            {
                flapSpeed = MathHelper.Lerp(0f, -200f, t.Eased);
            };
            Add(tween);
            yield break;
        }

        private void OnAnimate(string id)
        {
            if (!flyingAway && id == "flap" && sprite.CurrentAnimationFrame % 9 == 4)
            {
                Audio.Play("event:/game/general/strawberry_wingflap", Position);
                flapSpeed = -50f;
            }
            int num;
            if (id == "flap")
            {
                num = 25;
            }
            else
            {
                num = 30;
            }
            if (sprite.CurrentAnimationFrame == num)
            {
                lightTween.Start();
                Audio.Play("event:/game/general/strawberry_pulse", Position);
                SceneAs<Level>().Displacement.AddBurst(Position, 0.6f, 4f, 28f, (!collected && (CollideCheck<FakeWall>() || CollideCheck<Solid>())) ? 0.1f : 0.2f, null, null);
            }
        }

        private void OnCollect()
        {
            if (!collected)
            {
                int collectIndex = 0;
                collected = true;
                if (Follower.Leader != null)
                {
                    Player player = Follower.Leader.Entity as Player;
                    collectIndex = player.StrawberryCollectIndex;
                    player.StrawberryCollectIndex++;
                    player.StrawberryCollectResetTimer = 2.5f;
                    Follower.Leader.LoseFollower(Follower);
                    bool used = false;
                    foreach (Solid solid in Scene.Tracker.GetEntities<Solid>())
                    {
                        if (solid is LockBlock && Vector2.Distance(solid.Center, player.Center) <= 64f)
                        {
                            LockBlock door = solid as LockBlock;
                            used = true;
                            door.Add(new Coroutine(KeyBerryOpenDoor(door)));
                        }
                    }
                    foreach (IntroLockedCar car in Scene.Tracker.GetEntities<IntroLockedCar>())
                    {
                        if (Vector2.Distance(car.Center, player.Center) <= 64f)
                        {
                            used = true;
                            car.Add(new Coroutine(car.KeyberryUnlock()));
                        }
                    }
                    if (used)
                    {
                        SceneAs<Level>().Session.DoNotLoad.Add(ID);
                        VitModule.Session.keyberriesToReset.Add(ID);
                    }
                }
                Add(new Coroutine(CollectRoutine(collectIndex), true));
            }
        }

        private static IEnumerator KeyBerryOpenDoor(LockBlock door)
        {
            DynData<LockBlock> doorData = new DynData<LockBlock>(door);
            SoundEmitter emitter = SoundEmitter.Play(doorData.Get<string>("unlockSfxName"), door);
            emitter.Source.DisposeOnTransition = true;
            Level level = door.SceneAs<Level>();
            yield return 0.15f;
            door.UnlockingRegistered = true;
            if (doorData.Get<bool>("stepMusicProgress"))
            {
                AudioTrackState music = level.Session.Audio.Music;
                int progress = music.Progress;
                music.Progress = progress + 1;
                level.Session.Audio.Apply(false);
            }
            level.Session.DoNotLoad.Add(door.ID);
            VitModule.Session.doorsToReset.Add(door.ID);
            door.Tag |= Tags.TransitionUpdate;
            door.Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return doorData.Get<Sprite>("sprite").PlayRoutine("open", false);
            level.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return doorData.Get<Sprite>("sprite").PlayRoutine("burst", false);
            door.RemoveSelf();
            yield break;
        }

        private IEnumerator CollectRoutine(int collectIndex)
        {
            Level level = SceneAs<Level>();
            Tag = Tags.TransitionUpdate;
            Depth = -2000010;
            Audio.Play("event:/game/general/strawberry_get", Position, "colour", 0f, "count", (collectIndex == 5 ? 5 : 0));
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            sprite.Play("collect", false, false);
            while (sprite.Animating)
            {
                yield return null;
            }
            Scene.Add(new KeyBerryPoints(Position, collectIndex));
            if (collectIndex <= lastChain)
            {
                oneUpIDs.Clear();
            }
            if (collectIndex < 5)
            {
                oneUpIDs.Add(ID);
            }
            else if (collectIndex == 5)
            {
                foreach (EntityID entityID in oneUpIDs)
                {
                    level.Session.DoNotLoad.Add(entityID);
                    VitModule.Session.keyberriesToReset.Add(entityID);
                }
                bool used = false;
                foreach (Solid solid in Scene.Tracker.GetEntities<Solid>())
                {
                    if (solid is LockBlock)
                    {
                        used = true;
                        LockBlock door = solid as LockBlock;
                        door.Add(new Coroutine(KeyBerryOpenDoor(door)));
                    }
                }
                if (used)
                {
                    level.Session.DoNotLoad.Add(ID);
                    VitModule.Session.keyberriesToReset.Add(ID);
                }
            }
            lastChain = collectIndex;
            RemoveSelf();
            yield break;
        }

        public void CollectedSeeds()
        {
            waitingOnSeeds = false;
            Visible = true;
            Collidable = true;
            bloom.Visible = (light.Visible = true);
        }

        public bool winged;

        private BloomPoint bloom;

        private static int lastChain;

        private static List<EntityID> oneUpIDs = new List<EntityID>();

        private bool collected;

        private float collectTimer;

        private float flapSpeed;

        private bool flyingAway;

        public Follower Follower;

        public EntityID ID;

        private VertexLight light;

        private Tween lightTween;

        public bool returnHomeWhenLost;

        private Wiggler rotateWiggler;

        private bool hasReturnBubble;

        private Vector2 bubbleEnd;

        private Vector2 bubbleControl;

        public List<KeyBerrySeed> Seeds;

        private Sprite sprite;

        private Vector2 start;

        public bool waitingOnSeeds;

        private Wiggler wiggler;

        private float wobble;
    }
}
