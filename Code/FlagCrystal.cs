using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace vitmod
{
    [Tracked]
    [CustomEntity("vitellary/flagcrystal")]
    public class FlagCrystal : Actor
    {
        public FlagCrystal(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
        {
            ID = id;
            flag = data.Attr("flag", "");
            if (flag.StartsWith("!")) {
                flag = flag.Substring(1);
                invertFlag = true;
            }
            color = Calc.HexToColor(data.Attr("color", "cbdbfc"));
            isTheo = data.Bool("theo", false);
            spawnFlag = data.Attr("spawnFlag");
            hardVerticalHitSoundCooldown = 0f;
            previousPosition = Position;
            Depth = 100;
            Collider = new Hitbox(8f, 10f, -4f, -10f);
            Facing = Facings.Left;
            var customSprite = $"objects/{data.Attr("sprite", "flagcrystal")}";
            Add(backSprite = new Image(GFX.Game.Has($"{customSprite}/back") ? GFX.Game[$"{customSprite}/back"] : GFX.Game["objects/flagcrystal/back"]));
            Add(theoSprite = new Image(GFX.Game.Has($"{customSprite}/theo") ? GFX.Game[$"{customSprite}/theo"] : GFX.Game["objects/flagcrystal/theo"]));
            theoSprite.Visible = isTheo;
            Add(frontSprite = new Image(GFX.Game.Has($"{customSprite}/front") ? GFX.Game[$"{customSprite}/front"] : GFX.Game["objects/flagcrystal/front"]));
            backSprite.Scale.X = theoSprite.Scale.X = frontSprite.Scale.X = -1f;
            Vector2 spriteOffset = new Vector2(10f, 22f);
            backSprite.Origin = spriteOffset;
            theoSprite.Origin = spriteOffset;
            frontSprite.Origin = spriteOffset;
            backSprite.Color = color;
            frontSprite.Color = color;
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
            Hold.SlowFall = false;
            Hold.SlowRun = true;
            Hold.OnPickup = new Action(OnPickup);
            Hold.OnRelease = new Action<Vector2>(OnRelease);
            Hold.DangerousCheck = new Func<HoldableCollider, bool>(Dangerous);
            Hold.OnHitSeeker = new Action<Seeker>(HitSeeker);
            Hold.OnSwat = new Action<HoldableCollider, int>(Swat);
            Hold.OnHitSpring = new Func<Spring, bool>(HitSpring);
            Hold.OnHitSpinner = new Action<Entity>(HitSpinner);
            Hold.SpeedGetter = () => Speed;
            onCollideH = new Collision(OnCollideH);
            onCollideV = new Collision(OnCollideV);
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
            Tag = Tags.TransitionUpdate;
            Add(new MirrorReflection());
        }

        public static void Load()
        {
            On.Celeste.TempleGate.TheoIsNearby += TempleGate_TheoIsNearby;
        }

        public static void Unload()
        {
            On.Celeste.TempleGate.TheoIsNearby -= TempleGate_TheoIsNearby;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = scene as Level;
            if (!string.IsNullOrEmpty(spawnFlag) && !(scene as Level).Session.GetFlag(spawnFlag)) {
                RemoveSelf();
                return;
            }
            foreach (FlagCrystal crystal in level.Tracker.GetEntities<FlagCrystal>())
            {
                if (crystal != this && crystal.Hold.IsHeld && crystal.ID.Key == ID.Key)
                {
                    RemoveSelf();
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (!dead)
            {
                if (swatTimer > 0f)
                {
                    swatTimer -= Engine.DeltaTime;
                }
                hardVerticalHitSoundCooldown -= Engine.DeltaTime;
                if (Hold.IsHeld)
                {
                    prevLiftSpeed = Vector2.Zero;
                }
                else
                {
                    if (OnGround(1))
                    {
                        float target;
                        if (!OnGround(Position + Vector2.UnitX * 3f, 1))
                        {
                            target = 20f;
                        }
                        else if (!OnGround(Position - Vector2.UnitX * 3f, 1))
                        {
                            target = -20f;
                        }
                        else
                        {
                            target = 0f;
                        }
                        Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                        Vector2 liftSpeed = LiftSpeed;
                        if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                        {
                            Speed = prevLiftSpeed;
                            prevLiftSpeed = Vector2.Zero;
                            Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                            if (Speed.X != 0f && Speed.Y == 0f)
                            {
                                Speed.Y = -60f;
                            }
                            if (Speed.Y < 0f)
                            {
                                noGravityTimer = 0.15f;
                            }
                        }
                        else
                        {
                            prevLiftSpeed = liftSpeed;
                            if (liftSpeed.Y < 0f && Speed.Y < 0f)
                            {
                                Speed.Y = 0f;
                            }
                        }
                    }
                    else
                    {
                        if (Hold.ShouldHaveGravity)
                        {
                            float speedY = 800f;
                            if (Math.Abs(Speed.Y) <= 30f)
                            {
                                speedY *= 0.5f;
                            }
                            float speedX = 350f;
                            if (Speed.Y < 0f)
                            {
                                speedX *= 0.5f;
                            }
                            Speed.X = Calc.Approach(Speed.X, 0f, speedX * Engine.DeltaTime);
                            if (noGravityTimer > 0f)
                            {
                                noGravityTimer -= Engine.DeltaTime;
                            }
                            else
                            {
                                Speed.Y = Calc.Approach(Speed.Y, 200f, speedY * Engine.DeltaTime);
                            }
                        }
                    }
                    previousPosition = ExactPosition;
                    MoveH(Speed.X * Engine.DeltaTime, onCollideH, null);
                    MoveV(Speed.Y * Engine.DeltaTime, onCollideV, null);
                    if (Top > level.Bounds.Bottom && !level.Transitioning)
                    {
                        Die();
                    }
                }
                if (!dead)
                {
                    Hold.CheckAgainstColliders();
                }
                if (hitSeeker != null && swatTimer <= 0f && hitSeeker.Check(Hold))
                {
                    hitSeeker = null;
                }
            }
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
        }

        private void Swat(HoldableCollider hc, int dir)
        {
            if (Hold.IsHeld && hitSeeker == null)
            {
                swatTimer = 0.1f;
                hitSeeker = hc;
                Hold.Holder.Swat(dir);
            }
        }

        private bool Dangerous(HoldableCollider hc)
        {
            return !Hold.IsHeld && Speed != Vector2.Zero && hitSeeker != hc;
        }

        private void HitSeeker(Seeker seeker)
        {
            if (!Hold.IsHeld)
            {
                Speed = (Center - seeker.Center).SafeNormalize(120f);
            }
            Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
        }

        private void HitSpinner(Entity spinner)
        {
            if (!Hold.IsHeld && Speed.Length() < 0.01f && LiftSpeed.Length() < 0.01f && (previousPosition - ExactPosition).Length() < 0.01f && OnGround(1))
            {
                int dir = Math.Sign(X - spinner.X);
                if (dir == 0)
                {
                    dir = 1;
                }
                Speed.X = dir * 120f;
                Speed.Y = -30f;
            }
        }

        private bool HitSpring(Spring spring)
        {
            if (!Hold.IsHeld)
            {
                if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
                {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;
                }
                else if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f, null);
                    Speed.X = 220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                }
                else if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f, null);
                    Speed.X = -220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                }
                return true;
            }
            return false;
        }

        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }
            else if (data.Hit is PairedDashSwitch)
            {
                (data.Hit as PairedDashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }
            Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
            if (Math.Abs(Speed.X) > 100f)
            {
                ImpactParticles(data.Direction);
            }
            Speed.X = Speed.X * -0.4f;
        }

        private void OnCollideV(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }
            else if (data.Hit is PairedDashSwitch)
            {
                (data.Hit as PairedDashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }
            if (Speed.Y > 0f)
            {
                if (hardVerticalHitSoundCooldown <= 0f)
                {
                    Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f, 0f, 1f));
                    hardVerticalHitSoundCooldown = 0.5f;
                }
                else
                {
                    Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
                }
            }
            if (Speed.Y > 160f)
            {
                ImpactParticles(data.Direction);
            }
            if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch) && !(data.Hit is PairedDashSwitch))
            {
                Speed.Y = Speed.Y * -0.6f;
            }
            else
            {
                Speed.Y = 0f;
            }
        }

        private void ImpactParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f)
            {
                direction = 3.14159274f;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.X < 0f)
            {
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.Y > 0f)
            {
                direction = -1.57079637f;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
            }
            else
            {
                direction = 1.57079637f;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
            }
            level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
        }

        public override bool IsRiding(Solid solid)
        {
            return Speed.Y == 0f && base.IsRiding(solid);
        }

        protected override void OnSquish(CollisionData data)
        {
            if (!TrySquishWiggle(data))
            {
                Die();
            }
        }

        private void OnPickup()
        {
            level.Session.SetFlag(flag, !invertFlag);
            Speed = Vector2.Zero;
            Tag |= Tags.Persistent;
        }

        private void OnRelease(Vector2 force)
        {
            level.Session.SetFlag(flag, invertFlag);
            Tag &= ~Tags.Persistent;
            if (force.X != 0f && force.Y == 0f)
            {
                force.Y = -0.4f;
            }
            Speed = force * 200f;
            if (Speed != Vector2.Zero)
            {
                noGravityTimer = 0.1f;
            }
        }

        private void Die()
        {
            if (!dead)
            {
                dead = true;
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null && isTheo && !SaveData.Instance.Assists.Invincible)
                {
                    player.Die(-Vector2.UnitX * (float)player.Facing, false, true);
                }
                Audio.Play("event:/char/madeline/death", Position);
                Add(new DeathEffect(color, new Vector2?(Center - Position)));
                Collidable = false;
                if (Hold.Holder != null)
                    Hold.Holder.Holding = null;
                Hold.RemoveSelf();
                new DynData<Holdable>(Hold).Set<Player>("Holder", null);
                backSprite.Visible = false;
                theoSprite.Visible = false;
                frontSprite.Visible = false;
                level?.Session.SetFlag(flag, invertFlag);
                RemoveTag(Tags.TransitionUpdate | Tags.Persistent);
            }
        }

        public static bool TempleGate_TheoIsNearby(On.Celeste.TempleGate.orig_TheoIsNearby orig, TempleGate self)
        {
            if (self.Scene.Tracker.GetEntity<TheoCrystal>() != null)
            {
                return orig(self);
            }
            bool hasTheo = false;
            DynData<TempleGate> gateData = new DynData<TempleGate>(self);
            foreach (FlagCrystal crystal in self.SceneAs<Level>().Tracker.GetEntities<FlagCrystal>())
            {
                if (crystal.isTheo)
                {
                    hasTheo = true;
                    if (crystal.X > self.X + 10f || Vector2.DistanceSquared(gateData.Get<Vector2>("holdingCheckFrom"), crystal.Center) < (gateData.Get<bool>("open") ? 6400f : 4096f))
                    {
                        return true;
                    }
                }
            }
            return !hasTheo;
        }

        public EntityID ID;

        public Facings Facing;

        private string flag;

        private Color color;

        public bool isTheo;

        private string spawnFlag;

        private bool invertFlag;

        private Image frontSprite;

        private Image theoSprite;

        private Image backSprite;

        private float hardVerticalHitSoundCooldown;

        private Vector2 previousPosition;

        public Holdable Hold;

        private Vector2 Speed;

        private Collision onCollideH;

        private Collision onCollideV;

        private Vector2 prevLiftSpeed;

        private Level level;

        private bool dead;

        private float swatTimer;

        private float noGravityTimer;

        private HoldableCollider hitSeeker;
    }
}
