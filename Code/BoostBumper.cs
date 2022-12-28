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
    [CustomEntity("vitellary/boostbumper")]
    [Tracked]
    public class BoostBumper : Entity
    {
        public BoostBumper(Vector2 position) : base(position)
        {
            Depth = -8500;
            Collider = new Circle(10f, 0f, 2f);
            Add(sprite = GFX.SpriteBank.Create("boostBumper"));
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.5f, 16f));
            Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }, false, false));
            Add(new MirrorReflection());
        }

        public BoostBumper(EntityData data, Vector2 offset) : this(data.Position + offset) { }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Image image = new Image(GFX.Game["objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            outline = new Entity(Position);
            outline.Depth = 8999;
            outline.Visible = false;
            outline.Add(image);
            outline.Add(new MirrorReflection());
            scene.Add(outline);
        }

        public override void Update()
        {
            base.Update();
            if (cannotUseTimer > 0f)
            {
                cannotUseTimer -= Engine.DeltaTime;
            }
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
            if (respawnTimer <= 0f)
            {
                Vector2 target = Vector2.Zero;
                Player player = Scene.Tracker.GetEntity<Player>();
                if (player != null && CollideCheck(player))
                {
                    target = player.Center + new Vector2(0f, -2f) - Position;
                }
                sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
                if (Scene.OnInterval(0.05f))
                {
                    float dir = Calc.Random.NextAngle();
                    SceneAs<Level>().Particles.Emit(P_Idle, 1, Center + sprite.Position + Calc.AngleToVector(dir, 8f), Vector2.One * 2f, dir);
                }
            }
            if (sprite.CurrentAnimationID == "inside" && !CollideCheck<Player>())
            {
                sprite.Play("idle", false, false);
            }
        }

        public override void Render()
        {
            Vector2 position = sprite.Position;
            sprite.Position = position.Floor();
            if (sprite.CurrentAnimationID != "burst" && sprite.Visible)
            {
                sprite.DrawOutline(1);
            }
            base.Render();
            sprite.Position = position;
        }

        private void Respawn()
        {
            Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
            sprite.Position = Vector2.Zero;
            sprite.Play("idle", true, false);
            wiggler.Start();
            sprite.Visible = true;
            outline.Visible = false;
            AppearParticles();
        }

        private void AppearParticles()
        {
            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(P_Appear, 1, Center, Vector2.One * 2f, i * 0.0174532924f);
            }
        }

        private void OnPlayer(Player player)
        {
            if (respawnTimer <= 0f && cannotUseTimer <= 0f)
            {
                cannotUseTimer = 0.45f;
                player.StateMachine.State = 4;
                player.Speed = Vector2.Zero;
                DynData<Player> playerData = new DynData<Player>(player);
                playerData.Set("boostTarget", Center);
                startedBoosting = true;
                Audio.Play("event:/game/04_cliffside/greenbooster_enter", Position);
                wiggler.Start();
                sprite.Play("inside", false, false);
                sprite.FlipX = player.Facing == Facings.Left;
            }
        }

        public void PlayerBoosted(Player player, Vector2 direction)
        {
            startedBoosting = false;
            Vector2 launch = player.ExplodeLaunch(player.Center - direction, false, false);
            SceneAs<Level>().DirectionalShake(launch, 0.15f);
            SceneAs<Level>().Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f, null, null);
            SceneAs<Level>().Particles.Emit(Bumper.P_Launch, 12, Center + launch * 12f, Vector2.One * 3f, launch.Angle());
            sprite.Play("burst", false, false);
            outline.Visible = true;
            sprite.Visible = false;
            cannotUseTimer = 0f;
            respawnTimer = 1f;
            wiggler.Stop();
        }

        public static ParticleType P_Appear;

        public static ParticleType P_Idle;

        public Sprite sprite;

        private VertexLight light;

        private BloomPoint bloom;

        private Wiggler wiggler;

        private Entity outline;

        private float respawnTimer;

        private float cannotUseTimer;

        public bool startedBoosting;
    }
}
