using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace vitmod
{
	[Tracked]
	[CustomEntity("vitellary/custompuffer")]
	public class CustomPuffer : Actor
	{
		public enum States
		{
			Idle,
			Hit,
			Gone,
			Held
		}

		public enum BoostModes
        {
			SetSpeed,
			RedirectSpeed,
			AddRedirectSpeed
        }

		public Holdable Hold;
		public EntityID ID;
		public Vector2 Facing = Vector2.One;
		public States State;

		private Sprite sprite;
		private Sprite happySprite;
		private Vector2 startPosition;
		private Vector2 anchorPosition;
		private Vector2 lastSpeedPosition;
		private Vector2 lastSinePosition;
		private Circle pushRadius;
		private Circle breakWallsRadius;
		private Circle detectRadius;
		private SineWave idleSine;
		private Vector2 hitSpeed;
		private float goneTimer;
		private float cannotHitTimer;
		private Collision onCollideV;
		private Collision onCollideH;
		private float alertTimer;
		private Wiggler bounceWiggler;
		private Wiggler inflateWiggler;
		private Vector2 scale;
		private SimpleCurve returnCurve;
		private float cantExplodeTimer;
		private Vector2 lastPlayerPos;
		private float playerAliveFade;
		private float blastAngle;
		private float blastRadius;
		private float launchSpeed;
		private Player holder;
		private bool isHappy;
		private bool needsNewHome;
		private bool sameFace;
		private BoostModes boostMode;

		private float respawnTime = 2.5f;
		private float eyeSpin = 0f;
		private bool alwaysShowOutline = false;
		private bool isStatic = false;
		private bool pushAny = false;
		private bool oneUse = false;
		private string deathFlag = "";
		private Color outlineColor = Color.White;
		private bool canUpdateHome = false;
		private bool holdFlip = false;

		public CustomPuffer(Vector2 position, bool faceRight, float angle = 0f, float radius = 32f, float launchSpeed = 280f, string spriteName = "pufferFish")
			: base(position)
		{
			Collider = new Hitbox(12f, 10f, -6f, -5f);
			Depth = 1;
			Add(new PlayerCollider(OnPlayer, new Hitbox(14f, 12f, -7f, -7f)));
			Add(sprite = GFX.SpriteBank.Create(spriteName));
			sprite.Play("idle");
			Add(happySprite = GFX.SpriteBank.Create("flccSmileyPuffer"));
			happySprite.Play("idle");
			happySprite.Visible = false;
			if (!faceRight)
			{
				Facing.X = -1f;
			}
			idleSine = new SineWave(0.5f, 0f);
			idleSine.Randomize();
			Add(idleSine);
			anchorPosition = Position;
			Position += new Vector2(idleSine.Value * 3f, idleSine.ValueOverTwo * 2f);
			State = States.Idle;
			startPosition = (lastSinePosition = (lastSpeedPosition = Position));
			pushRadius = new Circle(radius + 8f);
			detectRadius = new Circle(radius);
			breakWallsRadius = new Circle(radius / 2f);
			blastRadius = radius;
			blastAngle =  WrapAngle(angle.ToRad());
			this.launchSpeed = launchSpeed;
			onCollideV = OnCollideV;
			onCollideH = OnCollideH;
			scale = Vector2.One;
			bounceWiggler = Wiggler.Create(0.6f, 2.5f, delegate (float v) {
				sprite.Rotation = v * 20f * ((float)Math.PI / 180f);
			});
			Add(bounceWiggler);
			inflateWiggler = Wiggler.Create(0.6f, 2f);
			Add(inflateWiggler);
		}

		public CustomPuffer(EntityData data, Vector2 offset, EntityID id)
			: this(data.Position + offset, data.Bool("right", false), data.Float("angle", 0f), data.Float("radius", 32f), data.Float("launchSpeed", 280f), data.Attr("sprite", "pufferFish"))
		{
			ID = id;

			respawnTime = data.Float("respawnTime", 2.5f);
			alwaysShowOutline = data.Bool("alwaysShowOutline");
			isStatic = data.Bool("static");
			pushAny = data.Bool("pushAnyDir");
			oneUse = data.Bool("oneUse");
			deathFlag = data.Attr("deathFlag");
			outlineColor = data.HexColor("outlineColor", Color.White);
			canUpdateHome = !data.Bool("returnToStart", true);
			holdFlip = data.Bool("holdFlip");
			boostMode = data.Enum("boostMode", BoostModes.SetSpeed);

			if (data.Bool("holdable"))
			{
				Add(Hold = new Holdable());
				Hold.PickupCollider = new Hitbox(20f, 18f, -10f, -10f);
				Hold.SlowFall = false;
				Hold.SlowRun = false;
				Hold.OnCarry = OnCarry;
				Hold.OnPickup = OnPickup;
				Hold.OnRelease = OnRelease;
				Hold.SpeedGetter = (() => hitSpeed);
				Add(new TransitionListener
				{
					OnOut = (f) => needsNewHome = true
				});
			}
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			if ((scene as Level).Session.GetFlag("pufferishappy"))
			{
				isHappy = true;
				sprite.Visible = false;
				happySprite.Visible = true;
			}
			foreach (CustomPuffer puffer in scene.Tracker.GetEntities<CustomPuffer>())
			{
				if (puffer != this && puffer.ID.Key == ID.Key && puffer.Hold.IsHeld)
				{
					RemoveSelf();
					return;
				}
			}
		}

		public override bool IsRiding(JumpThru jumpThru)
		{
			return false;
		}

		public override bool IsRiding(Solid solid)
		{
			return false;
		}

		private void OnCarry(Vector2 position)
		{
			Position = position - Vector2.UnitY * 8f;
			anchorPosition = Position;

			if (holdFlip)
			{
				var playerFacing = (float)Hold.Holder.Facing;
				if ((sameFace && playerFacing != Facing.X) || (!sameFace && playerFacing == Facing.X))
				{
					blastAngle = WrapAngle(-blastAngle);
					Facing.X *= -1f;
				}
			}
		}

		private void OnPickup()
		{
			Position = anchorPosition;
			hitSpeed = Vector2.Zero;
			State = States.Held;
			holder = Hold.Holder;
			sameFace = (float)Hold.Holder.Facing == Facing.X;
			AddTag(Tags.Persistent);
		}

		private void OnRelease(Vector2 force)
		{
			RemoveTag(Tags.Persistent);
			if (canUpdateHome)
				needsNewHome = true;
			if (force != Vector2.Zero)
			{
				GotoHitSpeed(force * new Vector2(240f, 200f));
				cannotHitTimer = 0.1f;
				cantExplodeTimer = 0.3f;
			}
			else
			{
				GotoIdle();

				if (holder != null)
				{
					var deg = WrapAngle(blastAngle + (45f).ToRad()).ToDeg() - 45f;
					if (!pushAny && !(deg >= 135f && deg <= 225f))
					{
						anchorPosition = (Position += Vector2.UnitY * 8f);
						OnPlayer(holder);
					}
					/*else if (pushAny)
					{
						var speed = holder.Speed;
						var lastAngle = blastAngle;
						if (holder.Speed.Y > 0f)
							blastAngle = 0f;
						else if (holder.Speed.X > 0f)
							blastAngle = (float)Math.PI * 1.5f;
						else if (holder.Speed.X < 0f)
							blastAngle = (float)Math.PI / 2f;
						else
							blastAngle = (float)Math.PI;
						if (blastAngle != (float)Math.PI)
						{
							pushAny = false;
							anchorPosition = (Position += Vector2.UnitY * 8f);
							OnPlayer(holder);
							pushAny = true;
						}
						blastAngle = lastAngle;
					}*/
				}
			}
			holder = null;
		}

		protected override void OnSquish(CollisionData data)
		{
			Explode();
			GotoGone();
		}

		private void OnCollideH(CollisionData data)
		{
			hitSpeed.X *= -0.8f;
		}

		private void OnCollideV(CollisionData data)
		{
			if (!(data.Direction.Y > 0f))
			{
				return;
			}
			for (int i = -1; i <= 1; i += 2)
			{
				for (int j = 1; j <= 2; j++)
				{
					Vector2 vector = Position + Vector2.UnitX * j * i;
					if (!CollideCheck<Solid>(vector) && !OnGround(vector))
					{
						Position = vector;
						return;
					}
				}
			}
			hitSpeed.Y *= -0.2f;
		}

		private void GotoIdle()
		{
			if (State == States.Gone)
			{
				Position = startPosition;
				cantExplodeTimer = 0.5f;
				sprite.Play("recover");
				Audio.Play("event:/new_content/game/10_farewell/puffer_reform", Position);
			}
			lastSinePosition = (lastSpeedPosition = (anchorPosition = Position));
			hitSpeed = Vector2.Zero;
			idleSine.Reset();
			State = States.Idle;
		}

		private void GotoHit(Vector2 from, float deg = 0f)
		{
			scale = new Vector2(1.2f, 0.8f);
			if (deg >= -45f && deg <= 45f)
			{ // top open
				hitSpeed = Vector2.UnitY * 200f;
			} else if (deg >= 135f && deg <= 225f)
			{ // bottom open
				hitSpeed = Vector2.UnitY * -150f;
			} else if (deg < 180f)
			{ // right open
				hitSpeed = Vector2.UnitX * -200f;
			} else
			{
				hitSpeed = Vector2.UnitX * 200f;
			}
			State = States.Hit;
			bounceWiggler.Start();
			Alert(restart: true, playSfx: false);
			Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
		}

		private void GotoHitSpeed(Vector2 speed)
		{
			hitSpeed = speed;
			State = States.Hit;
		}

		private void GotoGone()
		{
			Vector2 control = Position + (startPosition - Position) * 0.5f;
			if ((startPosition - Position).LengthSquared() > 100f)
			{
				if (Math.Abs(Position.Y - startPosition.Y) > Math.Abs(Position.X - startPosition.X))
				{
					if (Position.X > startPosition.X)
					{
						control += Vector2.UnitX * -24f;
					} else
					{
						control += Vector2.UnitX * 24f;
					}
				} else if (Position.Y > startPosition.Y)
				{
					control += Vector2.UnitY * -24f;
				} else
				{
					control += Vector2.UnitY * 24f;
				}
			}
			returnCurve = new SimpleCurve(Position, startPosition, control);
			Collidable = false;
			goneTimer = respawnTime;
			State = States.Gone;
		}

		private void Explode()
		{
			Collider collider = base.Collider;
			base.Collider = pushRadius;
			Audio.Play("event:/new_content/game/10_farewell/puffer_splode", Position);
			sprite.Play("explode");

			Player player = CollideFirst<Player>();
			if (player != null && !Scene.CollideCheck<Solid>(Position, player.Center))
				ExplodeLaunchPlayer(player);

			TheoCrystal theoCrystal = CollideFirst<TheoCrystal>();
			if (theoCrystal != null && !Scene.CollideCheck<Solid>(Position, theoCrystal.Center))
				theoCrystal.ExplodeLaunch(Position);

			foreach (TempleCrackedBlock entity in Scene.Tracker.GetEntities<TempleCrackedBlock>())
				if (CollideCheck(entity))
					entity.Break(Position);

			foreach (TouchSwitch entity in Scene.Tracker.GetEntities<TouchSwitch>())
				if (CollideCheck(entity))
					entity.TurnOn();

			foreach (CustomMovingTouchSwitch entity in Scene.Tracker.GetEntities<CustomMovingTouchSwitch>())
				if (CollideCheck(entity))
					entity.TurnOn();

			foreach (FloatingDebris entity in base.Scene.Tracker.GetEntities<FloatingDebris>())
				if (CollideCheck(entity))
					entity.OnExplode(Position);

			base.Collider = collider;
			Level level = SceneAs<Level>();
			level.Shake();
			level.Displacement.AddBurst(Position, 0.4f, 12f, 36f, 0.5f);
			level.Displacement.AddBurst(Position, 0.4f, 24f, 48f, 0.5f);
			level.Displacement.AddBurst(Position, 0.4f, 36f, 60f, 0.5f);
			for (float num = 0f; num < (float)Math.PI * 2f; num += 0.17453292f)
			{
				Vector2 position = base.Center + Calc.AngleToVector(num + Calc.Random.Range(-(float)Math.PI / 90f, (float)Math.PI / 90f), Calc.Random.Range(12, 18));
				level.Particles.Emit(Seeker.P_Regen, position, num);
			}

			if (needsNewHome)
			{
				needsNewHome = false;
				startPosition = anchorPosition;
			}

			if (!string.IsNullOrEmpty(deathFlag))
				level.Session.SetFlag(deathFlag);

			if (oneUse)
			{
				RemoveSelf();
			}
		}

		public override void Render()
		{
			sprite.Scale = scale * (1f + inflateWiggler.Value * 0.4f);
			sprite.Scale *= Facing;

			if (sprite.CurrentAnimationID != happySprite.CurrentAnimationID)
				happySprite.Play(sprite.CurrentAnimationID);
			happySprite.Scale = sprite.Scale;
			happySprite.Rotation = sprite.Rotation;

			bool flag = false;
			if (sprite.CurrentAnimationID != "hidden" && sprite.CurrentAnimationID != "explode" && sprite.CurrentAnimationID != "recover")
			{
				flag = true;
			} else if (sprite.CurrentAnimationID == "explode" && sprite.CurrentAnimationFrame <= 1)
			{
				flag = true;
			} else if (sprite.CurrentAnimationID == "recover" && sprite.CurrentAnimationFrame >= 4)
			{
				flag = true;
			}
			if (flag)
			{
				(isHappy ? happySprite : sprite).DrawSimpleOutline();
			}
			float num = playerAliveFade * Calc.ClampedMap((Position - lastPlayerPos).Length(), 128f, 96f);
			if ((num > 0f || alwaysShowOutline) && State != States.Gone && State != States.Held)
			{
				Vector2 value = lastPlayerPos;
				value.Y += value.Y - base.Y;
				value.X += value.X - base.X;
				float radiansB = (value - Position).Angle();
				int segments = (int)blastRadius - 4;
				for (int i = 0; i < segments; i++)
				{
					float num2 = (float)Math.Sin(base.Scene.TimeActive * 0.5f) * 0.02f;
					if (isStatic)
					{
						num2 = 0.01f;
					}
					float num3 = Calc.Map((float)i / (float)segments + num2, 0f, 1f, 0, (float)Math.PI);
					num3 += bounceWiggler.Value * 20f * ((float)Math.PI / 180f) + blastAngle;
					Vector2 value2 = Calc.AngleToVector(num3, 1f);
					Vector2 vector = Position + value2 * blastRadius;
					float t = 1f;
					if (!alwaysShowOutline)
					{
						t = Calc.ClampedMap(Calc.AbsAngleDiff(num3, radiansB), (float)Math.PI / 2f, 0.17453292f);
						t = Ease.CubeOut(t) * 0.8f * num;
					}
					if (t <= 0f)
					{
						continue;
					}
					if (i == 0 || i == segments-1)
					{
						Draw.Line(vector, vector - value2 * (blastRadius-12f), outlineColor * t);
						continue;
					}
					Vector2 vector2 = value2 * (float)Math.Sin(base.Scene.TimeActive * 2f + (float)i * 0.6f);
					if (i % 2 == 0)
					{
						vector2 *= -1f;
					}
					vector += vector2;
					if (Calc.AbsAngleDiff(num3, radiansB) <= 0.17453292f)
					{
						Draw.Line(vector, vector - value2 * 3f, outlineColor * t);
					} else
					{
						Draw.Point(vector, outlineColor * t);
					}
				}
			}
			base.Render();
			if (!isHappy && sprite.CurrentAnimationID == "alerted")
			{
				Vector2 vector3 = Position + new Vector2(3f, (Facing.X < 0f) ? (-5) : (-4)) * sprite.Scale;
				Vector2 to = lastPlayerPos + new Vector2(0f, -4f);
				float angleRadians = Calc.Angle(vector3, to) + eyeSpin * ((float)Math.PI * 2f) * 2f;
				Vector2 vector4 = Calc.AngleToVector(angleRadians, 1f);
				Vector2 vector5 = vector3 + new Vector2((float)Math.Round(vector4.X), (float)Math.Round(Calc.ClampedMap(vector4.Y, -1f, 1f, -1f, 2f)));
				Draw.Rect(vector5.X, vector5.Y, 1f, 1f, Color.Black);
			}
			sprite.Scale /= Facing;
		}

		public override void Update()
		{
			base.Update();
			eyeSpin = Calc.Approach(eyeSpin, 0f, Engine.DeltaTime * 1.5f);
			scale = Calc.Approach(scale, Vector2.One, 1f * Engine.DeltaTime);
			if (cannotHitTimer > 0f)
			{
				cannotHitTimer -= Engine.DeltaTime;
			}
			if (State != States.Gone && cantExplodeTimer > 0f)
			{
				cantExplodeTimer -= Engine.DeltaTime;
			}
			if (alertTimer > 0f)
			{
				alertTimer -= Engine.DeltaTime;
			}
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity == null)
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 0f, 1f * Engine.DeltaTime);
			} else
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 1f, 1f * Engine.DeltaTime);
				lastPlayerPos = entity.Center;
			}
			if (!isHappy && SceneAs<Level>().Session.GetFlag("pufferishappy"))
			{
				isHappy = true;
				sprite.Visible = false;
				happySprite.Visible = true;
			}
			else if (isHappy && !SceneAs<Level>().Session.GetFlag("pufferishappy"))
			{
				isHappy = false;
				sprite.Visible = true;
				happySprite.Visible = false;
			}
			switch (State)
			{
				case States.Idle:
				{
					if (Position != lastSinePosition)
					{
						anchorPosition += Position - lastSinePosition;
					}
					if (!isStatic)
					{
						Vector2 vector = anchorPosition + new Vector2(idleSine.Value * 3f, idleSine.ValueOverTwo * 2f);
						MoveToX(vector.X);
						MoveToY(vector.Y);
					}
					lastSinePosition = Position;
					if (needsNewHome)
					{
						needsNewHome = false;
						startPosition = anchorPosition;
					}
					if (ProximityExplodeCheck())
					{
						Explode();
						GotoGone();
						break;
					}
					if (AlertedCheck())
					{
						Alert(restart: false, playSfx: true);
					} else if (sprite.CurrentAnimationID == "alerted" && alertTimer <= 0f)
					{
						Audio.Play("event:/new_content/game/10_farewell/puffer_shrink", Position);
						sprite.Play("unalert");
					}
					foreach (CustomPufferCollider component in base.Scene.Tracker.GetComponents<CustomPufferCollider>())
					{
						component.Check(this);
					}
					break;
				}
				case States.Held:
					if (sprite.CurrentAnimationID != "alerted")
						Alert(restart: false, playSfx: true);
					break;
				case States.Hit:
					lastSpeedPosition = Position;
					MoveH(hitSpeed.X * Engine.DeltaTime, onCollideH);
					MoveV(hitSpeed.Y * Engine.DeltaTime, OnCollideV);
					anchorPosition = Position;
					hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
					hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);
					if (canUpdateHome)
						needsNewHome = true;
					if (ProximityExplodeCheck())
					{
						Explode();
						GotoGone();
						break;
					}
					if (base.Top >= (float)(SceneAs<Level>().Bounds.Bottom + 5))
					{
						sprite.Play("hidden");
						GotoGone();
						break;
					}
					foreach (CustomPufferCollider component2 in base.Scene.Tracker.GetComponents<CustomPufferCollider>())
					{
						component2.Check(this);
					}
					if (hitSpeed == Vector2.Zero)
					{
						ZeroRemainderX();
						ZeroRemainderY();
						GotoIdle();
					}
					break;
				case States.Gone:
				{
					float num = goneTimer;
					goneTimer -= Engine.DeltaTime;
					if (goneTimer <= 0.5f)
					{
						if (num > 0.5f && returnCurve.GetLengthParametric(8) > 8f)
						{
							Audio.Play("event:/new_content/game/10_farewell/puffer_return", Position);
						}
						Position = returnCurve.GetPoint(Ease.CubeInOut(Calc.ClampedMap(goneTimer, 0.5f, 0f)));
					}
					if (goneTimer <= 0f)
					{
						Visible = (Collidable = true);
						GotoIdle();
					}
					break;
				}
			}
		}

		public bool HitSpring(Spring spring)
		{
			switch (spring.Orientation)
			{
				default:
					if (hitSpeed.Y >= 0f)
					{
						GotoHitSpeed(224f * -Vector2.UnitY);
						MoveTowardsX(spring.CenterX, 4f);
						bounceWiggler.Start();
						Alert(restart: true, playSfx: false);
						return true;
					}
					return false;
				case Spring.Orientations.WallLeft:
					if (hitSpeed.X <= 60f)
					{
						Facing.X = 1f;
						GotoHitSpeed(280f * Vector2.UnitX);
						MoveTowardsY(spring.CenterY, 4f);
						bounceWiggler.Start();
						Alert(restart: true, playSfx: false);
						return true;
					}
					return false;
				case Spring.Orientations.WallRight:
					if (hitSpeed.X >= -60f)
					{
						Facing.X = -1f;
						GotoHitSpeed(280f * -Vector2.UnitX);
						MoveTowardsY(spring.CenterY, 4f);
						bounceWiggler.Start();
						Alert(restart: true, playSfx: false);
						return true;
					}
					return false;
			}
		}

		private bool ProximityExplodeCheck()
		{
			if (cantExplodeTimer > 0f || blastRadius == 0f)
			{
				return false;
			}
			bool result = false;
			Collider collider = base.Collider;
			base.Collider = detectRadius;
			Player player = CollideFirst<Player>();
			if (player != null)
			{
				float angle = (player.Center - Center).Angle();
				if (WrapAngle(angle - blastAngle) >= 0 && WrapAngle(angle - blastAngle) < Math.PI && !base.Scene.CollideCheck<Solid>(Position, player.Center))
				{
					result = true;
				}
			}
			base.Collider = collider;
			return result;
		}

		private bool AlertedCheck()
		{
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			return entity != null && (entity.Center - base.Center).Length() < blastRadius * 2f;
		}

		private void Alert(bool restart, bool playSfx)
		{
			if (sprite.CurrentAnimationID == "idle")
			{
				if (playSfx)
				{
					Audio.Play("event:/new_content/game/10_farewell/puffer_expand", Position);
				}
				sprite.Play("alert");
				inflateWiggler.Start();
			}
			else if (restart && playSfx)
			{
				Audio.Play("event:/new_content/game/10_farewell/puffer_expand", Position);
			}
			alertTimer = 2f;
		}

		private void OnPlayer(Player player)
		{
			if (State == States.Gone || State == States.Held || !(cantExplodeTimer <= 0f))
			{
				return;
			}
			if (Hold != null && Input.Grab.Check && !player.Ducking && !new DynData<Player>(player).Get<bool>("IsTired"))
				return;
			if (cannotHitTimer <= 0f)
			{
				Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));
				float baseAngle = blastAngle;
				if (pushAny)
				{
					baseAngle = (Center - player.Center).Angle() - (float)Math.PI/2;
				}
				float deg = WrapAngle(baseAngle + (45f).ToRad()).ToDeg() - 45f;
				if (deg >= -45f && deg <= 45f)
				{
					player.Bounce(Top);
				}
				else if (deg >= 135f && deg <= 225f)
				{
					player.MoveV(3);
					player.ReflectBounce(Vector2.UnitY);
				}
				else if (deg < 180f)
				{
					player.SideBounce(1, Right, CenterY);
				}
				else
				{
					player.SideBounce(-1, Left, CenterY);
				}
				GotoHit(player.Center, deg);
				MoveToX(anchorPosition.X);
				idleSine.Reset();
				anchorPosition = (lastSinePosition = Position);
				eyeSpin = 1f;
			}
			cannotHitTimer = 0.1f;
		}

		public Vector2 ExplodeLaunchPlayer(Player player)
		{
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			Celeste.Celeste.Freeze(0.1f);
			//player.launchApproachX = null;
			Vector2 vector = (player.Center - Position).SafeNormalize(-Vector2.UnitY);
			Vector2 sideVector = Calc.AngleToVector(blastAngle, 1f);
			float num = Vector2.Dot(vector, sideVector);

			sideVector *= Math.Sign(num);
			float oldSpeedX = Math.Abs(player.Speed.X);

			if (boostMode == BoostModes.RedirectSpeed)
            {
				player.Speed = oldSpeedX * sideVector;
            }
            else
            {
				player.Speed = launchSpeed * sideVector;
				if (boostMode == BoostModes.AddRedirectSpeed)
				{
					player.Speed += oldSpeedX * sideVector;
				}
			}
			if (player.Speed.Y <= 50f)
			{
				player.Speed.Y = Math.Min(Math.Max(-150f, -Math.Abs(launchSpeed)), player.Speed.Y);
				player.AutoJump = true;
			}
			if (Input.MoveX.Value == Math.Sign(player.Speed.X))
			{
				player.Speed.X *= 1.2f;
			}
			SlashFx.Burst(player.Center, player.Speed.Angle());
			if (!player.Inventory.NoRefills)
			{
				player.RefillDash();
			}
			player.RefillStamina();
			//player.dashCooldownTimer = 0.2f;
			player.StateMachine.State = 7;
			return vector;
		}

		private static float WrapAngle(float angle)
		{
			angle %= (float)Math.PI*2f;
			if (angle < 0)
			{
				angle += (float)Math.PI*2f;
			}
			return angle;
		}

		private static MethodInfo springBounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.Instance | BindingFlags.NonPublic);

		public static void Load()
		{
			On.Celeste.Spring.ctor_Vector2_Orientations_bool += Spring_ctor_Vector2_Orientations_bool;
		}

		public static void Unload()
		{
			On.Celeste.Spring.ctor_Vector2_Orientations_bool -= Spring_ctor_Vector2_Orientations_bool;
		}

		private static void Spring_ctor_Vector2_Orientations_bool(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 position, Spring.Orientations orientation, bool playerCanUse)
		{
			orig(self, position, orientation, playerCanUse);
			var collider = new CustomPufferCollider((p) => {
				if (p.HitSpring(self))
				{
					springBounceAnimate.Invoke(self, new object[] { });
				}
			});
			switch (self.Orientation)
			{
				case Spring.Orientations.Floor:
					collider.Collider = new Hitbox(16f, 10f, -8f, -10f); break;
				case Spring.Orientations.WallLeft:
					collider.Collider = new Hitbox(12f, 16f, 0f, -8f); break;
				case Spring.Orientations.WallRight:
					collider.Collider = new Hitbox(12f, 16f, -12f, -8f); break;
			}
			self.Add(collider);
		}
	}
}
