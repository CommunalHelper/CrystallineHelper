using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;

namespace vitmod
{
	[Tracked]
    [CustomEntity("vitellary/customtouchswitch")]
    public class CustomMovingTouchSwitch : Entity
    {
		public EntityID ID;
		private Vector2[] nodes;
		private Color inactiveColor;
		private Color movingColor;
		private Color activeColor;
		private Color finishColor;
		private Ease.Easer easer;
		private float moveTime;
		private bool allowDisable;
		private bool smoke;
		private bool badelineDeactivate;

		public ISwitch Switch;
		private Vector2 startPosition;
		private SoundSource touchSfx;
		private MTexture border = GFX.Game["objects/touchswitch/container"];
		private Sprite icon;
		private float ease;
		private Wiggler wiggler;
		private Vector2 pulse = Vector2.One;
		private float timer = 0f;
		private BloomPoint bloom;
		private bool moving;
		private int nodeIndex = 0;
		private ParticleType fireParticle;

		private Level level => (Level)Scene;

		public CustomMovingTouchSwitch(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
			ID = new EntityID(data.Level.Name, data.ID);
			nodes = data.NodesOffset(offset);
			var flag = data.Attr("flag");
			var inverted = data.Bool("inverted");
			var persistent = data.Bool("persistent");
			inactiveColor = data.HexColor("inactiveColor", Calc.HexToColor("5fcde4"));
			movingColor = data.HexColor("movingColor", Calc.HexToColor("ff7f7f"));
			activeColor = data.HexColor("activeColor", Color.White);
			finishColor = data.HexColor("finishColor", Calc.HexToColor("f141df"));
			easer = VitModule.EaseTypes[data.Attr("easing", "CubeOut")];
			moveTime = data.Float("moveTime", 1.25f);
			allowDisable = data.Bool("allowDisable");
			smoke = data.Bool("smoke", true);
			badelineDeactivate = data.Bool("badelineDeactivate");
			var iconName = data.Attr("icon", "vanilla");
			icon = new Sprite(GFX.Game, iconName == "vanilla" ? "objects/touchswitch/icon" : $"objects/customMovingTouchSwitch/{iconName}/icon");

			Depth = 2000;
			Switch = string.IsNullOrEmpty(flag) ? (ISwitch)new BasicSwitch() : (ISwitch)new FlagSwitch(flag, inverted, allowDisable);
			Add(Switch as Component);
			if (persistent)
				Switch.SetPersistent($"switch_{ID}");
			Add(new PlayerCollider(OnPlayer, null, new Hitbox(30f, 30f, -15f, -15f)));
			Add(icon);
			Add(bloom = new BloomPoint(0f, 16f));
			bloom.Alpha = 0f;
			icon.Add("idle", "", 0f, default(int));
			icon.Add("spin", "", 0.1f, new Chooser<string>("spin", 1f), 0, 1, 2, 3, 4, 5);
			icon.Play("spin");
			icon.Color = inactiveColor;
			icon.CenterOrigin();
			Collider = new Hitbox(16f, 16f, -8f, -8f);
			Add(new HoldableCollider(OnHoldable, new Hitbox(20f, 20f, -10f, -10f)));
			Add(new SeekerCollider(OnSeeker, new Hitbox(24f, 24f, -12f, -12f)));
			Add(wiggler = Wiggler.Create(0.5f, 4f, v => pulse = Vector2.One * (1f + v * 0.25f)));
			Add(new VertexLight(Color.White, 0.8f, 16, 32));
			Add(touchSfx = new SoundSource());
			fireParticle = new ParticleType(TouchSwitch.P_Fire) { Color = finishColor };
			startPosition = Position;

			Switch.OnActivate = () =>
			{
				wiggler.Start();
				for (int i = 0; i < 32; i++)
				{
					float num = Calc.Random.NextFloat((float)Math.PI * 2f);
					level.Particles.Emit(TouchSwitch.P_FireWhite, Position + Calc.AngleToVector(num, 6f), num);
				}
				icon.Rate = 4f;
			};
			Switch.OnFinish = () =>
			{
				ease = 0f;
			};
			Switch.OnDeactivate = () =>
			{
				Add(new Coroutine(ReturnRoutine()));
				icon.Rate = 1f;
				icon.Play("spin");
				icon.Color = inactiveColor;
				ease = 1f;
			};
			Switch.OnStartActive = () =>
			{
				if ((nodes?.Length ?? 0) > 0)
				{
					Position = nodes[nodes.Length-1];
					nodeIndex = nodes.Length;
				}
				icon.Rate = 4f;
				icon.Color = activeColor;
				ease = 1f;
			};
			Switch.OnStartFinished = () =>
			{
				if ((nodes?.Length ?? 0) > 0)
				{
					Position = nodes[nodes.Length-1];
					nodeIndex = nodes.Length;
				}
				icon.Rate = 0.1f;
				icon.Play("idle");
				icon.Color = finishColor;
				ease = 1f;
			};
		}

		public void TurnOn(bool undo = false)
		{
			Add(new Coroutine(TouchRoutine(undo)));
		}

		private void OnPlayer(Player player)
		{
			if (badelineDeactivate && player is InteractiveChaser.DummyPlayer)
			{
				if (Switch.Activated || Switch.Finished)
				{
					var onDeactivate = Switch.OnDeactivate;
					Switch.OnDeactivate = null;
					Switch.Deactivate();
					Switch.OnDeactivate = onDeactivate;
					icon.Rate = 1f;
					icon.Play("spin");
					icon.Color = inactiveColor;
					ease = 1f;
				}
				TurnOn(true);
			}
			else
			{
				TurnOn();
			}
		}

		private void OnHoldable(Holdable h)
		{
			TurnOn();
		}

		private void OnSeeker(Seeker seeker)
		{
			if (SceneAs<Level>().InsideCamera(Position, 10f))
			{
				TurnOn();
			}
		}

		private IEnumerator TouchRoutine(bool undo = false)
		{
			if (moving || Switch.Activated)
				yield break;
			moving = true;
			level.Shake(0.1f);
			if (!undo ? nodeIndex < (nodes?.Length ?? 0) : nodeIndex > 0)
			{
				Audio.Play("event:/game/general/crystalheart_bounce", Position);
				for (int i = 0; i < 16; i++)
				{
					float num = Calc.Random.NextFloat((float)Math.PI * 2f);
					level.Particles.Emit(TouchSwitch.P_FireWhite, Position + Calc.AngleToVector(num, 6f), num);
				}
				Vector2 start = Position;
				Vector2 target = (!undo || nodeIndex > 1) ? nodes[undo ? nodeIndex-2 : nodeIndex] : startPosition;
				float moveTimer = 0f;
				Add(new Coroutine(DrawPathParticles(Center, target + (Vector2.One * 7f))));
				while (moveTimer < moveTime)
				{
					moveTimer += Engine.DeltaTime;
					Position = start + ((target - start) * easer(moveTimer / moveTime));
					icon.Color = movingColor;
					yield return null;
				}
				Position = target;
				for (int i = 0; i < 32; i++)
				{
					float num = Calc.Random.NextFloat((float)Math.PI * 2f);
					level.Particles.Emit(TouchSwitch.P_FireWhite, Position + Calc.AngleToVector(num, 6f), num);
				}
				Audio.Play("event:/game/04_cliffside/greenbooster_dash", Position);
				if (undo)
					nodeIndex--;
				else
					nodeIndex++;
			}
			else if (!undo)
			{
				touchSfx.Play("event:/game/general/touchswitch_any");
				if (Switch.Activate())
				{
					SoundEmitter.Play("event:/game/general/touchswitch_last_oneshot");
					Add(new SoundSource("event:/game/general/touchswitch_last_cutoff"));
				}
			}
			moving = false;
			yield break;
		}

		private IEnumerator ReturnRoutine()
		{
			if (nodeIndex > 0)
			{
				nodeIndex = 0;
				Vector2 start = Position;
				Vector2 target = startPosition;
				float moveTimer = 0f;
				while (moveTimer < moveTime)
				{
					moveTimer += Engine.DeltaTime;
					Position = start + ((target - start) * easer(moveTimer / moveTime));
					yield return null;
				}
				Position = target;
			}
			yield break;
		}

		private IEnumerator DrawPathParticles(Vector2 start, Vector2 end)
		{
			Vector2 currentPosition = start;
			while (currentPosition != end)
			{
				currentPosition = new Vector2(Calc.LerpSnap(currentPosition.X, end.X, 0.5f, 5f), Calc.LerpSnap(currentPosition.Y, end.Y, 0.5f, 5f));
				if (nodeIndex + 1 == nodes.Length)
				{
					level.Particles.Emit(fireParticle, currentPosition);
				}
				else
				{
					level.Particles.Emit(TouchSwitch.P_FireWhite, currentPosition);
				}
				yield return null;
			}
		}

		public override void Update()
		{
			timer += Engine.DeltaTime * 8f;
			ease = Calc.Approach(ease, (Switch.Finished || Switch.Activated) ? 1f : 0f, Engine.DeltaTime * 2f);
			icon.Color = Color.Lerp(inactiveColor, Switch.Finished ? finishColor : activeColor, ease);
			icon.Color *= 0.5f + ((float)Math.Sin(timer) + 1f) / 2f * (1f - ease) * 0.5f + 0.5f * ease;
			bloom.Alpha = ease;
			if (Switch.Finished)
			{
				if (icon.Rate > 0.1f)
				{
					icon.Rate -= 2f * Engine.DeltaTime;
					if (icon.Rate <= 0.1f)
					{
						icon.Rate = 0.1f;
						wiggler.Start();
						icon.Play("idle");
						level.Displacement.AddBurst(Position, 0.6f, 4f, 28f, 0.2f);
					}
				}
				else if (smoke && Scene.OnInterval(0.03f))
				{
					Vector2 position = Position + new Vector2(0f, 1f) + Calc.AngleToVector(Calc.Random.NextAngle(), 5f);
					level.ParticlesBG.Emit(fireParticle, position);
				}
			}
			base.Update();
		}

		public override void Render()
		{
			border.DrawCentered(Position + new Vector2(0f, -1f), Color.Black);
			border.DrawCentered(Position, icon.Color, pulse);
			base.Render();
		}

		public interface ISwitch
		{
			bool Activated { get; set; }
			bool Finished { get; set; }

			Action OnActivate { get; set; }
			Action OnDeactivate { get; set; }
			Action OnFinish { get; set; }
			Action OnStartFinished { get; set; }
			Action OnStartActive { get; set; }

			void SetPersistent(string flag);
			bool Activate();
			void Deactivate();
			void Finish();
			void StartActive();
			void StartFinished();
		}

		public class BasicSwitch : Component, ISwitch
		{
			private Celeste.Switch _switch;
			private DynData<Celeste.Switch> switchData;
			private bool persistent;
			private string persistFlag;

			public BasicSwitch() : base(active: true, visible: false)
			{
				_switch = new Celeste.Switch(false);
				switchData = new DynData<Celeste.Switch>(_switch);
			}

			public override void Added(Entity entity)
			{
				base.Added(entity);
				entity.Add(_switch);
			}

			public override void EntityAdded(Scene scene)
			{
				base.EntityAdded(scene);
				if (persistent && SceneAs<Level>().Session.GetFlag(persistFlag))
					StartActive();
			}

			public void SetPersistent(string flag)
			{
				persistent = true;
				persistFlag = flag;
			}

			public bool Activate()
			{
				if (persistent)
					SceneAs<Level>().Session.SetFlag(persistFlag);
				return _switch.Activate();
			}
			public void Deactivate() => _switch.Deactivate();
			public void Finish() => _switch.Finish();
			public void StartFinished() => _switch.StartFinished();
			public void StartActive()
			{
				if (!Activated && !Finished)
				{
					Activated = true;
					OnStartActive?.Invoke();
				}
			}

			public bool Activated { get => _switch.Activated; set => switchData.Set("Activated", value); }
			public bool Finished { get => _switch.Finished; set => switchData.Set("Finished", value); }

			public Action OnActivate { get => _switch.OnActivate; set => _switch.OnActivate = value; }
			public Action OnDeactivate { get => _switch.OnDeactivate; set => _switch.OnDeactivate = value; }
			public Action OnFinish { get => _switch.OnFinish; set => _switch.OnFinish = value; }
			public Action OnStartFinished { get => _switch.OnStartFinished; set => _switch.OnStartFinished = value; }
			public Action OnStartActive { get; set; }
		}

		[Tracked(false)]
		public class FlagSwitch : Component, ISwitch
		{
			public string Flag;
			private bool inverted;
			private bool persistent;
			private string persistFlag;
			private bool allowDisable;

			public Action OnActivate { get; set; }
			public Action OnDeactivate { get; set; }
			public Action OnFinish { get; set; }
			public Action OnStartActive { get; set; }
			public Action OnStartFinished { get; set; }

			public bool Activated { get; set; }
			public bool Finished { get; set; }

			public FlagSwitch(string flag, bool inverted, bool allowDisable) : base(active: true, visible: false)
			{
				Flag = flag;
				this.inverted = inverted;
				this.allowDisable = allowDisable;
			}

			public override void EntityAdded(Scene scene)
			{
				base.EntityAdded(scene);
				if (inverted != SceneAs<Level>().Session.GetFlag(Flag))
					StartFinished();
				else if (persistent && SceneAs<Level>().Session.GetFlag(persistFlag))
					StartActive();
			}

			public override void Update()
			{
				if (Finished && allowDisable && inverted == SceneAs<Level>().Session.GetFlag(Flag))
					Deactivate();
			}

			public void SetPersistent(string flag)
			{
				persistent = true;
				persistFlag = flag;
			}

			public bool Activate()
			{
				if (!Finished && !Activated)
				{
					Activated = true;
					OnActivate?.Invoke();
					if (persistent)
						SceneAs<Level>().Session.SetFlag(persistFlag);
					return FinishedCheck(SceneAs<Level>(), Flag, inverted);
				}
				return false;
			}

			public void Deactivate()
			{
				if (Finished || Activated)
				{
					Finished = false;
					Activated = false;
					OnDeactivate?.Invoke();
					if (persistent)
						SceneAs<Level>().Session.SetFlag(persistFlag, false);
				}
			}

			public void Finish()
			{
				Finished = true;
				OnFinish?.Invoke();
			}

			public void StartFinished()
			{
				if (!Finished)
				{
					Finished = true;
					Activated = true;
					OnStartFinished?.Invoke();
				}
			}

			public void StartActive()
			{
				if (!Activated && !Finished)
				{
					Activated = true;
					OnStartActive?.Invoke();
				}
			}

			public static bool Check(Scene scene)
			{
				return scene.Tracker.GetComponents<FlagSwitch>().Any(e => (e as FlagSwitch).Finished);
			}

			private static bool FinishedCheck(Level level, string flag, bool inverted)
			{
				foreach (FlagSwitch flagSwitch in level.Tracker.GetComponents<FlagSwitch>())
				{
					if (flagSwitch.Flag == flag && !flagSwitch.Activated)
						return false;
				}
				foreach (FlagSwitch flagSwitch in level.Tracker.GetComponents<FlagSwitch>())
				{
					if (flagSwitch.Flag == flag)
						flagSwitch.Finish();
				}
				level.Session.SetFlag(flag, !inverted);
				return true;
			}
		}
	}
}
