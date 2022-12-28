using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace vitmod
{
	[Tracked]
    [CustomEntity("vitellary/paireddashswitch")]
    public class PairedDashSwitch : Solid
    {
		public enum Sides
		{
			Up,
			Down,
			Left,
			Right
		}

		//public static ParticleType P_PressA;
		//public static ParticleType P_PressB;
		//public static ParticleType P_PressAMirror;
		//public static ParticleType P_PressBMirror;

		private EntityID id;
		private Vector2? target;
		private Sides side;
		private bool startPressed;
		private bool affectedByFlag;
		private string flagName;

		private Vector2 origPosition;
		private bool pressed;
		private Vector2 pressDirection;
		private float pressCooldown;
		private float speedY;
		private float startY;
		private bool playerWasOn;
		private Sprite sprite;

		public string Group;
		public TempleGate ClaimedGate;

		public PairedDashSwitch(EntityData data, Vector2 offset) : base(data.Position + offset, 0f, 0f, safe: true)
		{
			id = new EntityID(data.Level.Name, data.ID);
			side = data.Enum("direction", Sides.Left);
			startPressed = data.Bool("pressed");
			flagName = data.Attr("flag");
			affectedByFlag = data.Bool("affectedByFlag");
			Group = data.Attr("groupId");

			Add(sprite = GFX.SpriteBank.Create(data.Attr("sprite", "dashSwitch_default")));
			sprite.Play("idle");
			var pushAnim = sprite.Animations["push"];
			sprite.Add("release", pushAnim.Delay, "idle", pushAnim.Frames);

			if (data.Nodes != null && data.Nodes.Length > 0)
				target = data.Nodes[0] + offset;

			if (side == Sides.Up || side == Sides.Down)
			{
				Collider.Width = 16f;
				Collider.Height = 8f;
			}
			else
			{
				Collider.Width = 8f;
				Collider.Height = 16f;
			}

			switch (side)
			{
				case Sides.Up:
					sprite.Position = new Vector2(8f, 8f);
					sprite.Rotation = (float)Math.PI / 2f;
					//pressedTarget = Position + Vector2.UnitY * 8f;
					pressDirection = Vector2.UnitY;
					startY = Y;
					break;
				case Sides.Down:
					sprite.Position = new Vector2(8f, 0f);
					sprite.Rotation = -(float)Math.PI / 2f;
					//pressedTarget = Position + Vector2.UnitY * -8f;
					pressDirection = -Vector2.UnitY;
					break;
				case Sides.Left:
					sprite.Position = new Vector2(8f, 8f);
					sprite.Rotation = 0f;
					//pressedTarget = Position + Vector2.UnitX * 8f;
					pressDirection = Vector2.UnitX;
					break;
				case Sides.Right:
					sprite.Position = new Vector2(0f, 8f);
					sprite.Rotation = (float)Math.PI;
					//pressedTarget = Position + Vector2.UnitX * -8f;
					pressDirection = -Vector2.UnitX;
					break;
			}
			origPosition = Position;
			OnDashCollide = OnDashed;
		}

		public override void Awake(Scene scene)
		{
			base.Awake(scene);

			if ((!affectedByFlag && startPressed) || (affectedByFlag && !string.IsNullOrEmpty(flagName) && SceneAs<Level>().Session.GetFlag(flagName)))
			{
				sprite.Play("pushed");
				Position = origPosition + pressDirection * 8f;
				sprite.Position -= pressDirection * 2f;
				pressed = true;

				TempleGate gate = GetGate();
				if (gate != null)
				{
					gate.StartOpen();
					DynData<TempleGate> gateData = new DynData<TempleGate>(gate);
					var pairedSwitches = gateData.Get<List<PairedDashSwitch>>("pairedSwitches");
					if (pairedSwitches == null)
					{
						pairedSwitches = new List<PairedDashSwitch>();
						gateData.Set("pairedSwitches", pairedSwitches);
					}
					if (!pairedSwitches.Contains(this))
						pairedSwitches.Add(this);
				}
			}

			if (!string.IsNullOrEmpty(flagName))
				(scene as Level).Session.SetFlag(flagName, pressed);
		}

		public override void Update()
		{
			base.Update();

			if (pressCooldown > 0f)
				pressCooldown -= Engine.DeltaTime;

			if (affectedByFlag && !string.IsNullOrEmpty(flagName))
			{
				if (!pressed && SceneAs<Level>().Session.GetFlag(flagName))
					Press(true);
				else if (pressed && !SceneAs<Level>().Session.GetFlag(flagName))
					Release();
			}

			Player playerOnTop = null;
			foreach (Actor actor in CollideAll<Actor>(Position - Vector2.UnitY)) {
				Holdable holdable = null;
				if (actor is Player player) {
					playerOnTop = player;
				} else if (side == Sides.Up && !pressed && (holdable = actor.Get<Holdable>()) != null && !holdable.IsHeld && holdable.SlowRun) {
					Press();
				}
			}

			if (pressed || side != Sides.Up)
				return;

			if (playerOnTop != null)
			{
				if (playerOnTop.Holding != null)
					Press();
				else
				{
					if (speedY < 0f)
						speedY = 0f;
					speedY = Calc.Approach(speedY, 70f, 200f * Engine.DeltaTime);
					if (Y < startY + 2f)
						MoveTowardsY(startY + 2f, speedY * Engine.DeltaTime);

					if (!playerWasOn)
						Audio.Play("event:/game/05_mirror_temple/button_depress", Position);
				}
			}
			else
			{
				if (speedY > 0f)
					speedY = 0f;
				speedY = Calc.Approach(speedY, -150f, 200f * Engine.DeltaTime);
				if (Y > startY)
					MoveTowardsY(startY, (0f - speedY) * Engine.DeltaTime);

				if (playerWasOn)
					Audio.Play("event:/game/05_mirror_temple/button_return", Position);
			}
			playerWasOn = playerOnTop != null;
		}

		public DashCollisionResults OnDashed(Player player, Vector2 direction)
		{
			if (direction == pressDirection) {
				Press(player != null);
			}
			return DashCollisionResults.NormalCollision;
		}

		public void Press(bool overrideCooldown = false)
		{
			if (overrideCooldown)
				pressCooldown = 0f;
			if (!pressed && pressCooldown <= 0f)
			{
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Audio.Play("event:/game/05_mirror_temple/button_activate", Position);
				sprite.Play("push");
				pressed = true;
				MoveTo(origPosition + pressDirection * 8f);
				sprite.Position -= pressDirection * 2f;
				SceneAs<Level>().ParticlesFG.Emit(DashSwitch.P_PressA, 10, Position + sprite.Position, pressDirection.Perpendicular() * 6f, sprite.Rotation - (float)Math.PI);
				SceneAs<Level>().ParticlesFG.Emit(DashSwitch.P_PressB, 4, Position + sprite.Position, pressDirection.Perpendicular() * 6f, sprite.Rotation - (float)Math.PI);

				TempleGate gate = GetGate();
				if (gate != null)
				{
					DynData<TempleGate> gateData = new DynData<TempleGate>(gate);
					if (!gateData.Get<bool>("open"))
						gate.Open();
					var pairedSwitches = gateData.Get<List<PairedDashSwitch>>("pairedSwitches");
					if (pairedSwitches == null)
					{
						pairedSwitches = new List<PairedDashSwitch>();
						gateData.Set("pairedSwitches", pairedSwitches);
					}
					if (!pairedSwitches.Contains(this))
						pairedSwitches.Add(this);
				}

				foreach (PairedDashSwitch dashSwitch in Scene.Tracker.GetEntities<PairedDashSwitch>())
					if (Group == dashSwitch.Group && dashSwitch != this)
						dashSwitch.Release();

				if (!string.IsNullOrEmpty(flagName))
					SceneAs<Level>().Session.SetFlag(flagName);
			}
		}

		public void Release()
		{
			if (pressed)
			{
				pressed = false;
				if (side == Sides.Up)
					pressCooldown = 0.15f;
				sprite.Play("release");
				MoveTo(origPosition + (side != Sides.Up || GetPlayerOnTop() == null ? Vector2.Zero : pressDirection * 2f));
				sprite.Position += pressDirection * 2f;

				if (ClaimedGate != null)
				{
					DynData<TempleGate> gateData = new DynData<TempleGate>(ClaimedGate);
					if (gateData.Get<bool>("open"))
					{
						var pairedSwitches = gateData.Get<List<PairedDashSwitch>>("pairedSwitches");
						if (pairedSwitches == null || (pairedSwitches != null && !pairedSwitches.Any((e) => e != this)))
						{
							ClaimedGate.ClaimedByASwitch = false;
							ClaimedGate.Close();
						}
						if (pairedSwitches != null)
							pairedSwitches.Remove(this);
					}
					ClaimedGate = null;
				}

				if (!string.IsNullOrEmpty(flagName))
					SceneAs<Level>().Session.SetFlag(flagName, false);
			}
		}

		private TempleGate GetGate()
		{
			if (!target.HasValue)
				return null;
			if (ClaimedGate != null)
				return ClaimedGate;
			List<Entity> entities = Scene.Tracker.GetEntities<TempleGate>();
			TempleGate templeGate = null;
			float minDist = 0f;
			foreach (TempleGate gate in entities)
			{
				if (gate.Type == TempleGate.Types.NearestSwitch && gate.LevelID == id.Level)
				{
					float dist = Vector2.DistanceSquared(target.Value, gate.Position);
					if (templeGate == null || dist < minDist)
					{
						templeGate = gate;
						minDist = dist;
					}
				}
			}
			if (templeGate != null)
			{
				templeGate.ClaimedByASwitch = true;
				ClaimedGate = templeGate;
			}
			return templeGate;
		}

		public static void Load()
		{
			On.Celeste.TheoCrystal.OnCollideV += TheoCrystal_OnCollideV;
			On.Celeste.TheoCrystal.OnCollideH += TheoCrystal_OnCollideH;
		}

		public static void Unload()
		{
			On.Celeste.TheoCrystal.OnCollideV -= TheoCrystal_OnCollideV;
			On.Celeste.TheoCrystal.OnCollideH -= TheoCrystal_OnCollideH;
		}

		private static void TheoCrystal_OnCollideV(On.Celeste.TheoCrystal.orig_OnCollideV orig, TheoCrystal self, CollisionData data)
		{
			if (data.Hit is PairedDashSwitch)
				(data.Hit as PairedDashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(self.Speed.Y));
			orig(self, data);
			if (data.Hit is PairedDashSwitch)
				self.Speed.Y = 0f;
		}

		private static void TheoCrystal_OnCollideH(On.Celeste.TheoCrystal.orig_OnCollideH orig, TheoCrystal self, CollisionData data)
		{
			if (data.Hit is PairedDashSwitch)
				(data.Hit as PairedDashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(self.Speed.X));
			orig(self, data);
		}
	}
}
