using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Platform = Celeste.Platform;

namespace vitmod
{
	[Tracked]
	[CustomEntity("vitellary/interactivechaser")]
    public class InteractiveChaser : Entity
    {
		public static readonly Color HairColor = Calc.HexToColor("9B3FB5");

		public PlayerSprite Sprite;
		public CustomPlayerHair Hair;
		private LightOcclude occlude;
		private bool ignorePlayerAnim;
		private Player player;
		private bool following;
		private float followBehindTime;
		private float followBehindIndexDelay;
		public bool Hovering;
		private float hoveringTimer;
		private Dictionary<string, SoundSource> loopingSounds;
		private List<SoundSource> inactiveLoopingSounds;
		private DummyPlayer dummyPlayer;

		// options
		public float FollowDelay;
		private bool canChangeMusic;
		private string flag;
		private bool harmful;
		private float startDelay;
		private Vector2 mirror;
		private string[] blacklist;

		public InteractiveChaser(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
			canChangeMusic = data.Bool("canChangeMusic");
			harmful = data.Bool("harmful", true);
			flag = data.Attr("flag");
			FollowDelay = data.Float("followDelay", 1.5f);
			startDelay = data.Float("startDelay", 0f);
			mirror = MirrorScales[data.Enum("mirroring", MirrorMode.None)];
			blacklist = data.Attr("blacklist").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			Hovering = false;
			hoveringTimer = 0f;
			loopingSounds = new Dictionary<string, SoundSource>();
			inactiveLoopingSounds = new List<SoundSource>();
			Depth = -1;
			Collider = new Hitbox(6f, 6f, -3f, mirror.Y == 1f ? -7f : 1f);
			Collidable = false;
			Sprite = new PlayerSprite(PlayerSpriteMode.Badeline);
			Sprite.Play("fallSlow", restart: true);
			Sprite.Scale.Y = mirror.Y;
			Hair = new CustomPlayerHair(Sprite, mirror);
			Hair.Color = HairColor;
			Hair.Border = Color.Black;
			Add(Hair);
			Add(Sprite);
			Visible = false;
			followBehindTime = FollowDelay;
			followBehindIndexDelay = startDelay;
			Add(new PlayerCollider(OnPlayer));
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			Add(new Coroutine(StartChasingRoutine(scene as Level)));
			scene.Add(dummyPlayer = new DummyPlayer(Position, mirror, blacklist));
		}

		public override void Removed(Scene scene)
		{
			dummyPlayer?.RemoveSelf();
			base.Removed(scene);
		}

		public IEnumerator StartChasingRoutine(Level level)
		{
			Hovering = true;
			while ((player = Scene.Tracker.GetEntity<Player>()) == null || player.JustRespawned || (!string.IsNullOrEmpty(flag) && !level.Session.GetFlag(flag)))
			{
				yield return null;
			}
			yield return followBehindIndexDelay;
			if (!Visible)
			{
				PopIntoExistance(0.2f);
			}
			Sprite.Play("fallSlow");
			Hair.Visible = true;
			Hovering = false;
			if (canChangeMusic)
			{
				level.Session.Audio.Music.Event = "event:/music/lvl2/chase";
				level.Session.Audio.Apply(forceSixteenthNoteHack: false);
			}
			yield return TweenToPlayer();
			Collidable = true;
			following = true;
			Add(occlude = new LightOcclude());
			if (!string.IsNullOrEmpty(flag) || level.Tracker.CountEntities<BadelineOldsiteEnd>() > 0)
			{
				Add(new Coroutine(StopChasing()));
			}
		}

		private IEnumerator TweenToPlayer()
		{
			Audio.Play("event:/char/badeline/level_entry", Position, "chaser_count", 0);
			if (followBehindTime > 0f)
			{
				Vector2 from = Position;
				Vector2 originalTo = player.Position;
				Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, Math.Max(0.5f, followBehindTime - 0.1f), start: true);
				tween.OnUpdate = delegate (Tween t)
				{
					if (player != null)
					{
						ChaserState chaseState;
						GetChasePosition(player, Scene.TimeActive, followBehindTime, out chaseState);
						Vector2 to = chaseState.Exists ? chaseState.Position : MirrorPos(originalTo, SceneAs<Level>(), mirror);
						Position = Vector2.Lerp(from, to, t.Eased);
						if (to.X != from.X)
						{
							Sprite.Scale.X = Math.Abs(Sprite.Scale.X) * (float)Math.Sign(to.X - from.X);
							Sprite.Scale.Y = mirror.Y;
						}
						Trail();
					}
				};
				Add(tween);
				yield return tween.Duration;
			}
			else
			{
				ChaserState chaseState;
				GetChasePosition(player, Scene.TimeActive, followBehindTime, out chaseState);
				Position = chaseState.Exists ? chaseState.Position : (FollowDelay == 0f ? MirrorPos(player.Position, SceneAs<Level>(), mirror) : Position);
				yield break;
			}
		}

		private IEnumerator StopChasing()
		{
			Level level = Scene as Level;
			while (!(string.IsNullOrEmpty(flag) || !level.Session.GetFlag(flag)) && !CollideCheck<BadelineOldsiteEnd>())
			{
				yield return null;
			}
			following = false;
			ignorePlayerAnim = true;
			Sprite.Play("laugh");
			Sprite.Scale.X = mirror.X;
			yield return 1f;
			Audio.Play("event:/char/badeline/disappear", Position);
			level.Displacement.AddBurst(Center, 0.5f, 24f, 96f, 0.4f);
			level.Particles.Emit(BadelineOldsite.P_Vanish, 12, Center, Vector2.One * 6f);
			RemoveSelf();
		}

		public override void Update()
		{
			ChaserState chaseState;
			if (player != null && player.Dead)
			{
				dummyPlayer.GrabState = GrabState.Drop;
				Sprite.Play("laugh");
				Sprite.X = (float)(Math.Sin(hoveringTimer) * 4.0);
				Hovering = true;
				hoveringTimer += Engine.DeltaTime * 2f;
				base.Depth = -12500;
				foreach (KeyValuePair<string, SoundSource> loopingSound in loopingSounds)
				{
					loopingSound.Value.Stop();
				}
				Trail();
			}
			else if (following && GetChasePosition(player, Scene.TimeActive, followBehindTime, out chaseState))
			{
				Position = chaseState.Position;
				//Position = Calc.Approach(Position, chaseState.Position, 1000f * Engine.DeltaTime);

				if (dummyPlayer != null)
				{
					dummyPlayer.UpdateState(chaseState);
				}

				if (!ignorePlayerAnim && chaseState.Animation != Sprite.CurrentAnimationID && chaseState.Animation != null && Sprite.Has(chaseState.Animation))
				{
					Sprite.Play(chaseState.Animation, restart: true);
				}
				if (!ignorePlayerAnim)
				{
					Sprite.Scale.X = Math.Abs(Sprite.Scale.X) * (float)chaseState.Facing;
					Sprite.Scale.Y = mirror.Y;
				}
				for (int i = 0; i < chaseState.Sounds; i++)
				{
					if (chaseState[i].Action == Player.ChaserStateSound.Actions.Oneshot)
					{
						Audio.Play(chaseState[i].Event, Position, chaseState[i].Parameter, chaseState[i].ParameterValue, "chaser_count", 0);
					}
					else if (chaseState[i].Action == Player.ChaserStateSound.Actions.Loop && !loopingSounds.ContainsKey(chaseState[i].Event))
					{
						SoundSource soundSource;
						if (inactiveLoopingSounds.Count > 0)
						{
							soundSource = inactiveLoopingSounds[0];
							inactiveLoopingSounds.RemoveAt(0);
						}
						else
						{
							Add(soundSource = new SoundSource());
						}
						soundSource.Play(chaseState[i].Event, "chaser_count", 0);
						loopingSounds.Add(chaseState[i].Event, soundSource);
					}
					else if (chaseState[i].Action == Player.ChaserStateSound.Actions.Stop)
					{
						SoundSource value = null;
						if (loopingSounds.TryGetValue(chaseState[i].Event, out value))
						{
							value.Stop();
							loopingSounds.Remove(chaseState[i].Event);
							inactiveLoopingSounds.Add(value);
						}
					}
				}

				Depth = chaseState.Depth;
				Trail();
			}
			if (Sprite.Scale.X != 0f)
			{
				Hair.Facing = (Facings)Math.Sign(Sprite.Scale.X);
			}
			if (Hovering)
			{
				hoveringTimer += Engine.DeltaTime;
				Sprite.Y = (float)(Math.Sin(hoveringTimer * 2f) * 4.0);
			}
			else
			{
				Sprite.Y = Calc.Approach(Sprite.Y, 0f, Engine.DeltaTime * 4f);
			}
			if (occlude != null)
			{
				occlude.Visible = !CollideCheck<Solid>();
			}

			base.Update();
		}

		private void Trail()
		{
			if (Scene.OnInterval(0.1f))
			{
				TrailManager.Add(this, Player.NormalHairColor, 1f);
			}
		}

		private void OnPlayer(Player player)
		{
			if (harmful && !(player is DummyPlayer))
				player.Die((player.Position - Position).SafeNormalize());
		}

		private void PopIntoExistance(float duration)
		{
			Visible = true;
			Sprite.Scale = Vector2.Zero;
			Sprite.Color = Color.Transparent;
			Hair.Visible = true;
			Hair.Alpha = 0f;
			Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, duration, start: true);
			tween.OnUpdate = delegate (Tween t)
			{
				Sprite.Scale = Vector2.One * t.Eased;
				Sprite.Color = Color.White * t.Eased;
				Hair.Alpha = t.Eased;
			};
			Add(tween);
		}

		private bool OnGround(int dist = 1)
		{
			for (int i = 1; i <= dist; i++)
			{
				if (CollideCheck<Solid>(Position + new Vector2(0f, i)))
				{
					return true;
				}
			}
			return false;
		}

		private bool GetChasePosition(Player player, float sceneTime, float timeAgo, out ChaserState chaseState)
		{
			if (!player.Dead)
			{
				var chaserStates = new DynData<Player>(player).Get<List<ChaserState>>("vitellaryInteractiveChaserStates");
				bool flag = false;
				foreach (ChaserState chaserState in chaserStates)
				{
					float num = sceneTime - chaserState.TimeStamp;
					if (num <= timeAgo)
					{
						if (flag || timeAgo - num < 0.02f)
						{
							chaseState = chaserState.Mirrored(SceneAs<Level>(), mirror);
							return true;
						}
						chaseState = default;
						return false;
					}
					flag = true;
				}
			}
			chaseState = default;
			return false;
		}

		public static Vector2 MirrorPos(Vector2 pos, Level level, Vector2 mirror)
		{
			var levelHeight = Math.Max(184, level.Bounds.Height);
			var levelCenter = new Vector2(level.Bounds.X + level.Bounds.Width / 2f, level.Bounds.Y + levelHeight / 2f);
			return (pos - levelCenter) * mirror + levelCenter;
		}

		public static void Load()
		{
			On.Celeste.Player.ctor += Player_ctor;
			On.Celeste.Player.Die += Player_Die;
			On.Celeste.Player.Update += Player_Update;
			On.Celeste.Actor.Update += Actor_Update;
			On.Celeste.Player.UpdateChaserStates += Player_UpdateChaserStates;
			On.Celeste.Player.OnTransition += Player_OnTransition;
		}

		public static void Unload()
		{
			On.Celeste.Player.ctor -= Player_ctor;
			On.Celeste.Player.Die -= Player_Die;
			On.Celeste.Player.Update -= Player_Update;
			On.Celeste.Actor.Update -= Actor_Update;
			On.Celeste.Player.UpdateChaserStates -= Player_UpdateChaserStates;
			On.Celeste.Player.OnTransition -= Player_OnTransition;
		}

		private static void PlayerDeadBody_ctor(On.Celeste.PlayerDeadBody.orig_ctor orig, PlayerDeadBody self, Player player, Vector2 direction)
		{
			orig(self, player, direction);
			if (player is DummyPlayer)
			{
				self.Active = false;
				self.Visible = false;
			}
		}

		private static void Player_OnTransition(On.Celeste.Player.orig_OnTransition orig, Player self)
		{
			var chaserStates = new DynData<Player>(self).Get<List<ChaserState>>("vitellaryInteractiveChaserStates");
			chaserStates.Clear();
			orig(self);
		}

		private static void Actor_Update(On.Celeste.Actor.orig_Update orig, Actor self)
		{
			orig(self);
			if (self is Player)
			{
				var player = self as Player;
				var playerData = new DynData<Player>(player);
				playerData.Set("vitellaryChaserPosition", player.Position);
				playerData.Set("vitellaryChaserSpeed", player.Speed);
				if (player.DashAttacking && player.Speed.Length() > 0f)
					playerData.Set("vitellaryChaserDashed", player.DashDir);
			}
		}

		private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
		{
			new DynData<Player>(self).Set("vitellaryChaserDashed", Vector2.Zero);
			orig(self);
		}

		private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
		{
			orig(self, position, spriteMode);
			var playerData = new DynData<Player>(self);
			playerData.Set("vitellaryInteractiveChaserStates", new List<ChaserState>());
			playerData.Set("vitellaryChaserPosition", self.Position);
			playerData.Set("vitellaryChaserSpeed", self.Speed);
		}

		private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
		{
			if (self is DummyPlayer)
				return null;
			return orig(self, direction, evenIfInvincible, registerDeathInStats);
		}

		private static void Player_UpdateChaserStates(On.Celeste.Player.orig_UpdateChaserStates orig, Player self)
		{
			var chasers = self.Scene.Tracker.GetEntities<InteractiveChaser>();
			if (chasers.Count > 0)
			{
				var maxDelay = chasers.Max(e => (e as InteractiveChaser).FollowDelay);
				var chaserStates = new DynData<Player>(self).Get<List<ChaserState>>("vitellaryInteractiveChaserStates");
				while (chaserStates.Count > 0 && self.Scene.TimeActive - chaserStates[0].TimeStamp > maxDelay)
					chaserStates.RemoveAt(0);
				chaserStates.Add(new ChaserState(self));
			}
			orig(self);
		}

		private static Dictionary<MirrorMode, Vector2> MirrorScales = new Dictionary<MirrorMode, Vector2>()
		{
			{ MirrorMode.None, new Vector2(1f, 1f) },
			{ MirrorMode.FlipH, new Vector2(-1f, 1f) },
			{ MirrorMode.FlipV, new Vector2(1f, -1f) },
			{ MirrorMode.FlipBoth, new Vector2(-1f, -1f) }
		};

		public enum MirrorMode
		{
			None,
			FlipH,
			FlipV,
			FlipBoth
		}

		[TrackedAs(typeof(Player))]
		public class DummyPlayer : Player
		{
			public GrabState GrabState { get; set; }
			public int State { get; set; }
			public string[] Blacklist { get; set; }

			private DynData<Player> baseData;
			private List<Component> newComponents;

			public DummyPlayer(Vector2 position, Vector2 mirror, string[] blacklist) : base(position, PlayerSpriteMode.Badeline)
			{
				baseData = new DynData<Player>(this);
				var hitboxes = new string[] { "normalHitbox", "duckHitbox", "normalHurtbox", "duckHurtbox" };
				foreach (var hitboxName in hitboxes)
				{
					var hitbox = baseData.Get<Hitbox>(hitboxName);
					hitbox.Center *= mirror;
				}
				Blacklist = blacklist;
			}

			public override void Added(Scene scene)
			{
				new DynData<Entity>(this).Set("Scene", scene);
				baseData.Set("level", scene as Level);
				Dashes = MaxDashes;

				newComponents = new List<Component>();
				newComponents.Add(Get<Leader>());
			}

			public override void Update()
			{
				newComponents.RemoveAll((c) => c.Entity == null);
				foreach (var component in newComponents)
					component.Update();

				var platform = (Platform)CollideFirst<Solid>(Position + Vector2.UnitY) ?? (Platform)CollideFirstOutside<JumpThru>(Position + Vector2.UnitY);
				if (platform != null)
				{
					baseData.Set("onGround", true);
					baseData.Set("OnSafeGround", platform.Safe);
				}
				else
				{
					baseData.Set("onGround", false);
					baseData.Set("OnSafeGround", false);
				}

				UpdateCarry();
			}
			public override void Render() { }

			public void UpdateState(InteractiveChaser.ChaserState state)
			{
				baseData.Set("dashAttackTimer", DashDir != Vector2.Zero ? 1f : 0f);
				Ducking = state.Ducking;
				GrabState = state.GrabState;
				Facing = state.Facing;
				DashDir = state.DashDir;
				if (DashDir != Vector2.Zero)
				{
					MoveToX(state.EarlyPosition.X + (state.Speed.X * Engine.DeltaTime), OnCollideH);
					MoveToY(state.EarlyPosition.Y + (state.Speed.Y * Engine.DeltaTime), OnCollideV);
				}
				State = state.State;
				Position = state.Position;

				if (GrabState == GrabState.Held)
					StateMachine.State = 1;
				else
					StateMachine.State = 0;

				if (GrabState == GrabState.Held && Holding == null)
				{
					foreach (Holdable holdable in Scene.Tracker.GetComponents<Holdable>())
					{
						if (!holdable.IsHeld && holdable.Check(this) && holdable.Pickup(this))
						{
							Holding = holdable;

							Vector2 begin = holdable.Entity.Position - Position;
							Vector2 end = new Vector2(0f, -12f);
							Vector2 control = new Vector2(begin.X + Math.Sign(begin.X) * 2f, -14f);
							SimpleCurve curve = new SimpleCurve(begin, end, control);

							baseData.Set("carryOffset", begin);
							var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, true);
							tween.OnUpdate = (t) => baseData.Set("carryOffset", curve.GetPoint(t.Eased));
							AddNew(tween);
						}
					}
				}
				else if (GrabState != GrabState.Held && Holding != null)
				{
					if (GrabState == GrabState.Drop)
						Holding.Release(Vector2.Zero);
					else
						Holding.Release(Vector2.UnitX * (float)Facing);
					Holding = null;
				}
				else if (Holding != null && Holding.Holder != this) {
					Holding = null;
				}

				var collider = Collider;
				Collider = baseData.Get<Hitbox>("hurtbox");
				foreach (PlayerCollider playerCollider in Scene.Tracker.GetComponents<PlayerCollider>())
				{
					if (playerCollider.Entity == null || !Blacklist.Any((s) => s == playerCollider.Entity.GetType().Name))
						playerCollider.Check(this);
				}
				Collider = collider;

				UpdateCarry();
			}

			private void OnCollideH(CollisionData data)
			{
				if (data.Hit != null && data.Direction.X == Math.Sign(DashDir.X) && !Blacklist.Any((s) => s == data.Hit.GetType().Name))
					data.Hit.OnDashCollide?.Invoke(this, data.Direction);
			}

			private void OnCollideV(CollisionData data)
			{
				if (data.Hit != null && data.Direction.Y == Math.Sign(DashDir.Y) && !Blacklist.Any((s) => s == data.Hit.GetType().Name))
					data.Hit.OnDashCollide?.Invoke(this, data.Direction);
			}

			private void AddNew(Component c)
			{
				Add(c);
				newComponents.Add(c);
			}
		}

		public struct ChaserState
		{
			public bool Exists;
			public Vector2 Bottom;
			public Vector2 Position;
			public Vector2 EarlyPosition;
			public float TimeStamp;
			public string Animation;
			public Facings Facing;
			public bool OnGround;
			public Color HairColor;
			public int Depth;
			public Vector2 Scale;
			public Vector2 Speed;
			public Vector2 DashDir;
			public bool Ducking;
			public int State;
			public GrabState GrabState;

			private Player.ChaserStateSound sound0;
			private Player.ChaserStateSound sound1;
			private Player.ChaserStateSound sound2;
			private Player.ChaserStateSound sound3;
			private Player.ChaserStateSound sound4;

			public int Sounds;

			public Player.ChaserStateSound this[int index]
			{
				get
				{
					switch (index)
					{
						case 0:
							return sound0;
						case 1:
							return sound1;
						case 2:
							return sound2;
						case 3:
							return sound3;
						case 4:
							return sound4;
						default:
							return default;
					}
				}
			}

			public ChaserState(Player player)
			{
				var playerData = new DynData<Player>(player);
				Exists = true;
				Bottom = player.BottomCenter;
				Position = player.Position;
				TimeStamp = player.Scene.TimeActive;
				Animation = player.Sprite.CurrentAnimationID;
				Facing = player.Facing;
				OnGround = playerData.Get<bool>("onGround");
				HairColor = player.Hair.Color;
				Depth = player.Depth;
				Scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
				EarlyPosition = playerData.Get<Vector2>("vitellaryChaserPosition");
				Speed = playerData.Get<Vector2>("vitellaryChaserSpeed");
				DashDir = playerData.Get<Vector2>("vitellaryChaserDashed");
				Ducking = player.Ducking;
				State = player.StateMachine.State;
				GrabState = player.Ducking ? GrabState.Drop : ((Input.Grab.Check && DashDir == Vector2.Zero) ? GrabState.Held : (Input.MoveY == 1 ? GrabState.Drop : GrabState.None));
				List<Player.ChaserStateSound> activeSounds = playerData.Get<List<Player.ChaserStateSound>>("activeSounds");
				Sounds = Math.Min(5, activeSounds.Count);
				sound0 = ((Sounds > 0) ? activeSounds[0] : default);
				sound1 = ((Sounds > 1) ? activeSounds[1] : default);
				sound2 = ((Sounds > 2) ? activeSounds[2] : default);
				sound3 = ((Sounds > 3) ? activeSounds[3] : default);
				sound4 = ((Sounds > 4) ? activeSounds[4] : default);
			}

			public ChaserState Mirrored(Level level, Vector2 scales)
			{
				var newState = this;

				newState.Position = MirrorPos(Position, level, scales);
				newState.EarlyPosition = MirrorPos(EarlyPosition, level, scales);
				newState.Speed *= scales;
				newState.DashDir *= scales;
				newState.Facing = (Facings)((int)Facing * (int)scales.X);

				return newState;
			}
		}

		public enum GrabState
		{
			None,
			Held,
			Drop
		}
	}
}