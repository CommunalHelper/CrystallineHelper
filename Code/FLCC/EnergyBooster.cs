using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace vitmod
{
	[CustomEntity("vitellary/energybooster")]
    public class EnergyBooster : Entity
    {
        private const string vitellaryInEnergyBooster = "vitellaryInEnergyBooster";
		private const float RespawnTime = 1f;
		public static readonly Vector2 playerOffset = new Vector2(0f, -2f);

		private Sprite sprite;
		private Entity outline;
		private Wiggler wiggler;
		private BloomPoint bloom;
		private VertexLight light;
		private Coroutine dashRoutine;
		private DashListener dashListener;
		private ParticleType particleType;
		private ParticleType appearParticle;
		private float respawnTimer;
		private float cannotUseTimer;
		private SoundSource loopingSfx;
		private bool dashBehavior;
		private bool redirectSpeed;
		private bool oneUse;

		public Vector2 PlayerSpeed;

		public Player BoostingPlayer
		{
			get;
			private set;
		}

		public EnergyBooster(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
			dashBehavior = data.Bool("behaveLikeDash");
			redirectSpeed = data.Bool("redirectSpeed");
			oneUse = data.Bool("oneUse");

			Depth = -8500;
			Collider = new Circle(10f, 0f, 2f);
			Add(sprite = GFX.SpriteBank.Create(!redirectSpeed ? "crystalline_flcc_energyBooster" : "crystalline_flcc_energyBoosterRedirect"));
			Add(new PlayerCollider(OnPlayer));
			Add(light = new VertexLight(Color.White, 1f, 16, 32));
			Add(bloom = new BloomPoint(0.1f, 16f));
			Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
			{
				sprite.Scale = Vector2.One * (1f + f * 0.25f);
			}));
			Add(dashRoutine = new Coroutine(removeOnComplete: false));
			Add(dashListener = new DashListener());
			Add(new MirrorReflection());
			Add(loopingSfx = new SoundSource());
			dashListener.OnDash = OnPlayerDashed;
			particleType = new ParticleType(Booster.P_Burst) { Color = Calc.HexToColor(redirectSpeed ? "5439B5" : "FFF051") };
			appearParticle = new ParticleType(Booster.P_Appear) { Color = Calc.HexToColor(redirectSpeed ? "C7B5FF" : "FFF6A8") };
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			Image image = new Image(GFX.Game[!redirectSpeed ? "objects/CrystallineHelper/FLCC/energyBooster/outline" : "objects/CrystallineHelper/FLCC/energyBoosterRedirect/outline"]);
			image.CenterOrigin();
			image.Color = Color.White * 0.75f;
			outline = new Entity(Position);
			outline.Depth = 8999;
			outline.Visible = false;
			outline.Add(image);
			outline.Add(new MirrorReflection());
			scene.Add(outline);
		}

		public void Appear()
		{
			Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
			sprite.Play("appear");
			wiggler.Start();
			Visible = true;
			AppearParticles();
		}

		private void AppearParticles()
		{
			ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
			for (int i = 0; i < 360; i += 30)
			{
				particlesBG.Emit(appearParticle, 1, base.Center, Vector2.One * 2f, (float)i * ((float)Math.PI / 180f));
			}
		}

		private void OnPlayer(Player player)
		{
			if (respawnTimer <= 0f && cannotUseTimer <= 0f && BoostingPlayer == null)
			{
				cannotUseTimer = 0.45f;
				player.boostRed = false;
				player.StateMachine.State = 4;
				PlayerSpeed = player.Speed;
				if (player.LiftSpeed != Vector2.Zero)
					PlayerSpeed += player.LiftSpeed;
				player.Speed = Vector2.Zero;
				player.boostTarget = Center;
                DynamicData.For(player).Set(vitellaryInEnergyBooster, this);
				Audio.Play("event:/game/04_cliffside/greenbooster_enter", Position);
				wiggler.Start();
				sprite.Play("inside");
				sprite.FlipX = player.Facing == Facings.Left;
			}
		}

		public void PlayerBoosted(Player player, Vector2 direction)
		{
			Audio.Play("event:/game/04_cliffside/greenbooster_dash", Position);
			BoostingPlayer = player;
			base.Tag = (int)Tags.Persistent | (int)Tags.TransitionUpdate;
			sprite.Play("spin");
			sprite.FlipX = player.Facing == Facings.Left;
			Vector2 newSpeed = Vector2.Zero;
			if (!redirectSpeed)
			{
				Vector2 baseSpeed = direction.SafeNormalize(240f);
				newSpeed = new Vector2(Math.Max(Math.Abs(baseSpeed.X), Math.Abs(PlayerSpeed.X)) * Math.Sign(direction.X), Math.Max(Math.Abs(baseSpeed.Y), Math.Abs(PlayerSpeed.Y)) * Math.Sign(direction.Y));
			}
			else
			{
				newSpeed = direction.SafeNormalize() * PlayerSpeed.Length();
			}
			player.Speed = newSpeed;
			if (!oneUse) {
				outline.Visible = true;
			}
			wiggler.Start();
			//PlayerReleased(player);

			dashRoutine.Replace(BoostRoutine(player, direction, newSpeed));
		}

		private IEnumerator BoostRoutine(Player player, Vector2 dir, Vector2 speed)
		{
			float angle = (-dir).Angle();
			while ((player.StateMachine.State == 5 || player.StateMachine.State == 2) && BoostingPlayer != null)
			{
				sprite.RenderPosition = player.Center + playerOffset;
				loopingSfx.Position = sprite.Position;
				if (Scene.OnInterval(0.02f))
				{
					(Scene as Level).ParticlesBG.Emit(particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), angle);
				}
				yield return null;
			}
			PlayerReleased(player);
			if (player.StateMachine.State == 4)
			{
				sprite.Visible = false;
			}
			else if (!dashBehavior)
			{
				player.StateMachine.State = 0;
				player.Speed = speed;
			}
			while (SceneAs<Level>().Transitioning)
			{
				yield return null;
			}
			Tag = 0;
		}

		public void OnPlayerDashed(Vector2 direction)
		{
			if (BoostingPlayer != null)
			{
				BoostingPlayer = null;
			}
		}

		public void PlayerReleased(Player player = null)
		{
			Audio.Play("event:/game/04_cliffside/greenbooster_end", sprite.RenderPosition);
			sprite.Play("pop");
			cannotUseTimer = 0f;
			respawnTimer = 1f;
			BoostingPlayer = null;
			if (player != null)
			{
                DynamicData playerData = DynamicData.For(player);
                EnergyBooster currentBooster = playerData.Get<EnergyBooster>(vitellaryInEnergyBooster);
				if (currentBooster == this)
					playerData.Set(vitellaryInEnergyBooster, null);
				else
					currentBooster.PlayerSpeed = PlayerSpeed;
			}
			wiggler.Stop();
			loopingSfx.Stop();
			if (oneUse) {
				sprite.OnFinish = (f) => RemoveSelf();
			}
		}

		public void PlayerDied()
		{
			if (BoostingPlayer != null)
			{
				PlayerReleased(BoostingPlayer);
				dashRoutine.Active = false;
				Tag = 0;
			}
		}

		public bool Respawn()
		{
			if (oneUse) {
				RemoveSelf();
				return false;
			}
			Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
			sprite.Position = Vector2.Zero;
			sprite.Play("loop", restart: true);
			wiggler.Start();
			sprite.Visible = true;
			outline.Visible = false;
			AppearParticles();
			return true;
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
					if (!Respawn()) {
						return;
					}
				}
			}
			if (!dashRoutine.Active && respawnTimer <= 0f)
			{
				Vector2 target = Vector2.Zero;
				Player entity = base.Scene.Tracker.GetEntity<Player>();
				if (entity != null && CollideCheck(entity))
				{
					target = entity.Center + playerOffset - Position;
				}
				sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
			}
			if (sprite.CurrentAnimationID == "inside" && BoostingPlayer == null && !CollideCheck<Player>())
			{
				sprite.Play("loop");
			}
		}

		public override void Render()
		{
			Vector2 position = sprite.Position;
			sprite.Position = position.Floor();
			if (sprite.CurrentAnimationID != "pop" && sprite.Visible)
			{
				sprite.DrawOutline();
			}
			base.Render();
			sprite.Position = position;
		}

		public static void Load()
		{
			On.Celeste.Player.CallDashEvents += Player_CallDashEvents;
			On.Celeste.Player.Die += Player_Die;
		}

		public static void Unload()
		{
			On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
			On.Celeste.Player.Die -= Player_Die;
		}

		private static void Player_CallDashEvents(On.Celeste.Player.orig_CallDashEvents orig, Player self)
		{
			DynamicData playerData = DynamicData.For(self);
			if (!self.calledDashEvents)
			{
				var booster = playerData.Get<EnergyBooster>(vitellaryInEnergyBooster);
				if (booster != null)
				{
					booster.PlayerBoosted(self, self.DashDir);
                    self.calledDashEvents = true;
				}
				else
				{
					orig(self);
				}
			}
		}

		private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
		{
			PlayerDeadBody body = orig(self, direction, evenIfInvincible, registerDeathInStats);
			if (body != null)
			{
				EnergyBooster booster = DynamicData.For(self).Get<EnergyBooster>(vitellaryInEnergyBooster);
                booster?.PlayerDied();
            }
			return body;
		}
	}
}
