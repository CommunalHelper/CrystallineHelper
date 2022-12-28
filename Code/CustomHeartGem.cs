using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace vitmod
{
	[CustomEntity("vitellary/customheart")]
    public class CustomHeartGem : Entity
    {
		public enum SpriteType
		{
			Blue,
			Red,
			Gold,
			Custom,
			Core,
			CoreInverted,
			Random
		}

		public static ParticleType P_Shatter = new ParticleType
		{
			Color = Color.Blue,
			Color2 = Color.White,
			ColorMode = ParticleType.ColorModes.Blink,
			FadeMode = ParticleType.FadeModes.Late,
			LifeMin = 0.25f,
			LifeMax = 0.4f,
			Size = 1f,
			Direction = 0f,
			DirectionRange = (float)Math.PI*2f,
			SpeedMin = 50f,
			SpeedMax = 80f,
			SpeedMultiplier = 0.005f,
		};

		public Wiggler ScaleWiggler;

		private bool endLevel;
		private bool oneUse;
		private bool slowdown;
		private float respawnTime;
		private string poemId;
		private SpriteType spriteType;
		private string spritePath;
		private string spriteColor;
		private float bloomStr;
		private bool hasLight;
		private bool bully;
		private bool additionalEffects;
		private bool switchCoreMode;
		private bool colorGrade;
		private bool isStatic;

		private EntityID entityID;
		private Sprite sprite;
		private Sprite coldSprite;
		private Sprite white;
		private Sprite outline;
		private ParticleType shineParticle;
		private ParticleType breakParticle;
		private Wiggler moveWiggler;
		private Vector2 moveWiggleDir;
		private BloomPoint bloom;
		private VertexLight light;
		private Poem poem;
		private float timer;
		private bool collected;
		private bool collecting;
		private bool autoPulse;
		private float bounceSfxDelay;
		private float respawnTimer;
		private int dashCount;
		private SoundEmitter sfx;
		private List<InvisibleBarrier> walls;
		private HoldableCollider holdableCollider;

		public CustomHeartGem(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
			entityID = new EntityID(data.Level.Name, data.ID);
			slowdown = data.Bool("slowdown");
			endLevel = data.Bool("endLevel");
			oneUse = data.Bool("oneUse");
			respawnTime = data.Float("respawnTime", -1f);
			dashCount = data.Int("dashCount", 1);
			poemId = data.Attr("poemId");
			spriteType = data.Enum<SpriteType>("type");
			spritePath = $"collectables/{data.Attr("path", "heartGemColorable")}/";
			spriteColor = data.Attr("color", "ffffff");
			bloomStr = data.Float("bloom", 0.75f);
			hasLight = data.Bool("light", true);
			bully = data.Bool("bully");
			additionalEffects = data.Bool("additionalEffects", true);
			switchCoreMode = data.Bool("switchCoreMode");
			colorGrade = data.Bool("colorGrade");
			isStatic = data.Bool("static");

			autoPulse = true;
			walls = new List<InvisibleBarrier>();
			if (!bully)
			{
				Add(holdableCollider = new HoldableCollider(OnHoldable));
			}
			Add(new MirrorReflection());
		}

		public override void Awake(Scene scene)
		{
			base.Awake(scene);
			Level level = base.Scene as Level;

			if (level.Session.DoNotLoad.Contains(entityID))
			{
				RemoveSelf();
				return;
			}

			if (spriteType == SpriteType.Random)
				spriteType = Calc.Random.Choose(SpriteType.Blue, SpriteType.Red, SpriteType.Gold);

			if (spriteType == SpriteType.Custom)
			{
				Add(sprite = CreateSprite(""));
				sprite.SetColor(Calc.HexToColor(spriteColor));
				if (GFX.Game.Has($"{spritePath}outline00"))
				{
					Add(outline = CreateSprite("outline"));
				}
			}
			else if (spriteType == SpriteType.Blue)
			{
				Add(sprite = GFX.SpriteBank.Create("heartgem0"));
			}
			else if (spriteType == SpriteType.Red)
			{
				Add(sprite = GFX.SpriteBank.Create("heartgem1"));
			}
			else if (spriteType == SpriteType.Gold)
			{
				Add(sprite = GFX.SpriteBank.Create("heartgem2"));
			}
			else if (spriteType == SpriteType.Core)
			{
				Add(sprite = GFX.SpriteBank.Create("heartgem1"));
				Add(coldSprite = GFX.SpriteBank.Create("heartgem0"));
				if (level.CoreMode == Session.CoreModes.Cold)
					sprite.Visible = false;
				else
					coldSprite.Visible = false;
			}
			else if (spriteType == SpriteType.CoreInverted)
			{
				Add(sprite = GFX.SpriteBank.Create("heartgem0"));
				Add(coldSprite = GFX.SpriteBank.Create("heartgem1"));
				if (level.CoreMode == Session.CoreModes.Cold)
					sprite.Visible = false;
				else
					coldSprite.Visible = false;
			}
			sprite.Play("spin");
			if (coldSprite != null && coldSprite.Visible)
				coldSprite.Play("spin");
			sprite.OnLoop = delegate (string anim)
			{
				if (Visible && anim == "spin" && autoPulse)
				{
					Audio.Play("event:/game/general/crystalheart_pulse", Position);
					ScaleWiggler.Start();
					(base.Scene as Level).Displacement.AddBurst(Position, 0.35f, 8f, 48f, 0.25f);
				}
			};
			Collider = new Hitbox(16f, 16f, -8f, -8f);
			Add(new PlayerCollider(OnPlayer));
			Add(ScaleWiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
			{
				sprite.Scale = Vector2.One * (1f + f * 0.25f);
			}));
			Add(bloom = new BloomPoint(bloomStr, 16f));
			Color value = Color.White;
			if (spriteType == SpriteType.Custom)
			{
				value = Calc.HexToColor(spriteColor);
				shineParticle = new ParticleType(HeartGem.P_BlueShine)
				{
					Color = value
				};
			}
			else if (spriteType == SpriteType.Blue || spriteType == SpriteType.CoreInverted)
			{
				value = Color.Aqua;
				shineParticle = HeartGem.P_BlueShine;
			}
			else if (spriteType == SpriteType.Red || spriteType == SpriteType.Core)
			{
				value = Color.Red;
				shineParticle = HeartGem.P_RedShine;
			}
			else if (spriteType == SpriteType.Gold)
			{
				value = Color.Gold;
				shineParticle = HeartGem.P_GoldShine;
			}
			breakParticle = new ParticleType(P_Shatter)
			{
				Color = shineParticle.Color
			};
			value = Color.Lerp(value, Color.White, 0.5f);
			Add(light = new VertexLight(value, hasLight ? 1f : 0f, 32, 64));
			moveWiggler = Wiggler.Create(0.8f, 2f);
			moveWiggler.StartZero = true;
			Add(moveWiggler);
		}

		private Sprite CreateSprite(string path)
		{
			var sprite = new Sprite(GFX.SpriteBank.Atlas, spritePath);
			sprite.AddLoop("idle", path, 0f, new int[] { 0 });
			sprite.AddLoop("spin", path, 0.1f, Calc.ReadCSVIntWithTricks("0*10,1-13"));
			sprite.AddLoop("fastspin", path, 0.1f);
			sprite.CenterOrigin();
			sprite.Justify = new Vector2(0.5f, 0.5f);
			sprite.Play("idle");
			return sprite;
		}

		public override void Update()
		{
			bounceSfxDelay -= Engine.DeltaTime;
			timer += Engine.DeltaTime;

			if (collected && respawnTimer > 0f)
				respawnTimer -= Engine.DeltaTime;
			if (collected && respawnTimer <= 0f)
			{
				respawnTimer = 0f;
				collected = false;
				Collidable = true;
				Visible = true;
				if (additionalEffects)
				{
					Audio.Play("event:/game/general/diamond_return", Position);
					bloom.Alpha = bloomStr;
					light.Alpha = (hasLight ? 1f : 0f);
				}
				ScaleWiggler.Start();
			}

			if (spriteType == SpriteType.Core || spriteType == SpriteType.CoreInverted)
			{
				if (SceneAs<Level>().CoreMode == Session.CoreModes.Cold)
				{
					sprite.Visible = false;
					coldSprite.Visible = true;
					shineParticle = (spriteType == SpriteType.Core ? HeartGem.P_BlueShine : HeartGem.P_RedShine);
				}
				else
				{
					sprite.Visible = true;
					coldSprite.Visible = false;
					shineParticle = (spriteType == SpriteType.Core ? HeartGem.P_RedShine : HeartGem.P_BlueShine);
				}
			}

			if (collecting && (Scene.Tracker.GetEntity<Player>()?.Dead ?? true))
			{
				EndCutscene();
			}

			base.Update();

			if (!isStatic)
				sprite.Position = Vector2.UnitY * (float)Math.Sin(timer * 2f) * 2f + moveWiggleDir * moveWiggler.Value * -8f;
			var sprites = new List<Sprite>();
			if (coldSprite != null)
				sprites.Add(coldSprite);
			if (outline != null)
				sprites.Add(outline);
			if (white != null)
				sprites.Add(white);
			foreach (var other in sprites)
			{
				other.Position = sprite.Position;
				other.Scale = sprite.Scale;
				if (other.CurrentAnimationID != sprite.CurrentAnimationID)
				{
					other.Play(sprite.CurrentAnimationID);
				}
				other.SetAnimationFrame(sprite.CurrentAnimationFrame);
			}

			if (!collecting && Visible && Scene.OnInterval(0.1f))
			{
				SceneAs<Level>().Particles.Emit(shineParticle, 1, base.Center, Vector2.One * 8f);
			}
		}

		public void OnHoldable(Holdable h)
		{
			Player entity = Scene.Tracker.GetEntity<Player>();
			if (!collected && entity != null && h.Dangerous(holdableCollider))
			{
				Collect(entity, h.GetSpeed().Angle());
			}
		}

		public void OnPlayer(Player player)
		{
			if (collected || (base.Scene as Level).Frozen)
			{
				return;
			}
			if (player.DashAttacking && !bully)
			{
				Collect(player, player.Speed.Angle());
				return;
			}
			if (bounceSfxDelay <= 0f)
			{
				Audio.Play("event:/game/general/crystalheart_bounce", Position);
				bounceSfxDelay = 0.1f;
			}
			player.PointBounce(base.Center);
			if (dashCount != 1)
            {
				player.Dashes = dashCount;
            }
			moveWiggler.Start();
			ScaleWiggler.Start();
			moveWiggleDir = (base.Center - player.Center).SafeNormalize(Vector2.UnitY);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
		}

		private void Collect(Player player, float angle)
		{
			if (Collidable)
			{
				if (switchCoreMode)
				{
					Level level = SceneAs<Level>();
					if (spriteType == SpriteType.Blue)
						level.CoreMode = Session.CoreModes.Cold;
					else if (spriteType == SpriteType.Red)
						level.CoreMode = Session.CoreModes.Hot;
					else
						level.CoreMode = (level.CoreMode == Session.CoreModes.Cold ? Session.CoreModes.Hot : Session.CoreModes.Cold);
					if (oneUse)
					{
						level.Session.CoreMode = level.CoreMode;
					}
					if (colorGrade)
					{
						if (level.CoreMode == Session.CoreModes.Cold)
							level.SnapColorGrade("cold");
						else if (level.CoreMode == Session.CoreModes.Hot)
							level.SnapColorGrade("hot");
					}
				}
				if (!endLevel)
				{
					player?.RefillDash();
				}
				if (slowdown)
				{
					Scene.Tracker.GetEntity<AngryOshiro>()?.StopControllingTime();
					Coroutine coroutine = new Coroutine(CollectRoutine(player));
					coroutine.UseRawDeltaTime = true;
					Add(coroutine);
					collecting = true;
				}
				else
				{
					Celeste.Celeste.Freeze(0.05f);
					if (additionalEffects)
					{
						Audio.Play("event:/game/general/diamond_touch", Position);
						SceneAs<Level>().Particles.Emit(breakParticle, 8, Center, Vector2.One * 8f);
						light.Alpha = 0f;
						bloom.Alpha = 0f;
					}
					SceneAs<Level>().Shake(0.3f);
					SlashFx.Burst(Position, angle);
					Visible = false;
					PostCollect();
				}
				Collidable = false;
			}
		}

		private IEnumerator CollectRoutine(Player player)
		{
			Level level = Scene as Level;
			AreaKey area = level.Session.Area;
			level.CanRetry = false;
			if (endLevel)
			{
				Audio.SetMusic(null);
				Audio.SetAmbience(null);
				List<IStrawberry> list = new List<IStrawberry>();
				ReadOnlyCollection<Type> berryTypes = StrawberryRegistry.GetBerryTypes();
				foreach (Follower follower in player.Leader.Followers)
				{
					if (berryTypes.Contains(follower.Entity.GetType()) && follower.Entity is IStrawberry)
					{
						list.Add(follower.Entity as IStrawberry);
					}
				}
				foreach (IStrawberry item in list)
				{
					item.OnCollect();
				}
			}

			string sfxEvent;
			if (area.Mode == AreaMode.BSide)
			{
				sfxEvent = "event:/game/general/crystalheart_red_get";
			}
			else if (area.Mode == AreaMode.CSide)
			{
				sfxEvent = "event:/game/general/crystalheart_gold_get";
			}
			else
			{
				sfxEvent = "event:/game/general/crystalheart_blue_get";
			}

			sfx = SoundEmitter.Play(sfxEvent, this);
			Add(new LevelEndingHook(delegate
			{
				sfx.Source.Stop();
			}));
			walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Right, level.Bounds.Top), 8f, level.Bounds.Height));
			walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Left - 8, level.Bounds.Top), 8f, level.Bounds.Height));
			walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Left, level.Bounds.Top - 8), level.Bounds.Width, 8f));
			foreach (InvisibleBarrier wall in walls)
			{
				Scene.Add(wall);
			}
			if (spriteType == SpriteType.Custom && GFX.Game.Has($"{spritePath}white00"))
			{
				Add(white = CreateSprite("white"));
			}
			else
			{
				Add(white = GFX.SpriteBank.Create("heartGemWhite"));
			}
			Depth = -2000000;
			yield return null;
			Celeste.Celeste.Freeze(0.2f);
			yield return null;
			Engine.TimeRate = 0.5f;
			player.Depth = -2000000;
			for (int i = 0; i < 10; i++)
			{
				Scene.Add(new AbsorbOrb(Position));
			}
			level.Shake();
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			level.Flash(Color.White);
			level.FormationBackdrop.Display = true;
			level.FormationBackdrop.Alpha = 1f;
			light.Alpha = (bloom.Alpha = 0f);
			Visible = false;
			for (float t3 = 0f; t3 < 2f; t3 += Engine.RawDeltaTime)
			{
				Engine.TimeRate = Calc.Approach(Engine.TimeRate, 0f, Engine.RawDeltaTime * 0.25f);
				yield return null;
			}
			yield return null;
			if (player.Dead)
			{
				yield return 100f;
			}
			Engine.TimeRate = 1f;
			Tag = Tags.FrozenUpdate;
			level.Frozen = true;
			if (endLevel)
			{
				RegisterAsCollected(level);
				level.TimerStopped = true;
				level.RegisterAreaComplete();
			}
			string poemText = null;
			if (!string.IsNullOrEmpty(poemId))
			{
				poemText = Dialog.Clean("poem_" + poemId);
			}
			int heartIndex;
			switch (spriteType)
			{
				default:
					heartIndex = 3; break;
				case SpriteType.Blue:
					heartIndex = 0; break;
				case SpriteType.Red:
					heartIndex = 1; break;
				case SpriteType.Gold:
					heartIndex = 2; break;
				case SpriteType.Core:
					heartIndex = (level.CoreMode == Session.CoreModes.Cold ? 0 : 1); break;
				case SpriteType.CoreInverted:
					heartIndex = (level.CoreMode == Session.CoreModes.Cold ? 1 : 0); break;
			}
			poem = new Poem(poemText, heartIndex, 0.8f);
			poem.Alpha = 0f;
			Scene.Add(poem);
			if (spriteType == SpriteType.Custom)
			{
				poem.Heart.SetColor(Calc.HexToColor(spriteColor));
			}
			for (float t2 = 0f; t2 < 1f; t2 += Engine.RawDeltaTime)
			{
				poem.Alpha = Ease.CubeOut(t2);
				yield return null;
			}
			while (!Input.MenuConfirm.Pressed && !Input.MenuCancel.Pressed)
			{
				yield return null;
			}
			sfx.Source.Param("end", 1f);
			if (!endLevel)
			{
				level.FormationBackdrop.Display = false;
				for (float t = 0f; t < 1f; t += Engine.RawDeltaTime * 2f)
				{
					poem.Alpha = Ease.CubeIn(1f - t);
					yield return null;
				}
				player.Depth = 0;
				EndCutscene();
				PostCollect();
			}
			else
			{
				yield return new FadeWipe(level, wipeIn: false)
				{
					Duration = 3.25f
				}.Duration;
				level.CompleteArea(spotlightWipe: false, skipScreenWipe: true, skipCompleteScreen: false);
			}
		}

		private void EndCutscene()
		{
			Level level = base.Scene as Level;
			level.Frozen = false;
			level.CanRetry = true;
			level.FormationBackdrop.Display = false;
			Engine.TimeRate = 1f;
			if (poem != null)
			{
				poem.RemoveSelf();
			}
			foreach (InvisibleBarrier wall in walls)
			{
				wall.RemoveSelf();
			}
			collecting = false;
		}

		private void PostCollect()
		{
			collected = true;
			if (respawnTime >= 0f && !oneUse && !endLevel)
			{
				respawnTimer = respawnTime;
				Remove(white);
				white = null;
				Visible = false;
			}
			else
			{
				if (oneUse)
				{
					SceneAs<Level>().Session.DoNotLoad.Add(entityID);
				}
				if (endLevel)
				{
					Level level = SceneAs<Level>();
					RegisterAsCollected(level);
					level.TimerStopped = true;
					level.RegisterAreaComplete();
					level.CompleteArea(spotlightWipe: false, skipCompleteScreen: false);
				}
				RemoveSelf();
			}
		}

		private void RegisterAsCollected(Level level)
		{
			level.Session.HeartGem = true;
			level.Session.UpdateLevelStartDashes();
			SaveData.Instance.RegisterHeartGem(level.Session.Area);
			if (!string.IsNullOrEmpty(poemId))
			{
				SaveData.Instance.RegisterPoemEntry(poemId);
			}
		}
	}
}
