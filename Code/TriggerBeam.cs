using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace vitmod
{
    [Tracked]
    [CustomEntity("vitellary/triggerbeam")]
    public class TriggerBeam : Entity
    {
        public static ParticleType P_Beam;

        private Vector2[] nodes;
        private Color color;
        private Color inactiveColor;
        private Vector2 direction;
        private string flag;
        private bool invertFlag;
        private bool exitAlwaysActive;
        private List<Trigger> triggers;
        private List<Trigger> exitTriggers;
        private List<int> exitNodes;
        private float size;

        private bool hadPlayer;

        public TriggerBeam(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            nodes = data.NodesOffset(offset);
            var baseColor = data.HexColor("color", Color.White);
            color = baseColor * data.Float("alpha", 0.5f);
            inactiveColor = baseColor * data.Float("inactiveAlpha", 0f);
            direction = Directions[data.Attr("direction", "Up")];
            flag = data.Attr("flag");
            invertFlag = data.Bool("invertFlag");
            exitAlwaysActive = data.Bool("exitAlwaysActive");
            var exitNodesRaw = data.Attr("exitNodes").Split(',');
            if (!(exitNodesRaw.Length == 1 && exitNodesRaw[0] == ""))
                exitNodes = exitNodesRaw.Select((s) => int.Parse(s)).ToList();
            else
                exitNodes = new List<int>();

            Depth = 8500;
            size = direction.Y != 0f ? data.Width : data.Height;
            Collider = new Hitbox(size, 1f, 0f, -1f);

            var isStatic = !data.Bool("attachToSolids");
            if (!isStatic)
            {
                Add(new StaticMover
                {
                    SolidChecker = IsRiding,
                    JumpThruChecker = IsRiding,
                    OnDisable = () =>
                    {
                        var player = Scene.Tracker.GetEntity<Player>();
                        if (player != null)
                            TriggerExitAll(player);
                    },
                    OnDestroy = () =>
                    {
                        var player = Scene.Tracker.GetEntity<Player>();
                        if (player != null)
                            TriggerLeave(player);
                        RemoveSelf();
                    }
                });
            }

            for (int i = 0; i < size / 8; i++)
            {
                var side = direction.Perpendicular().Abs();
                Add(new Beam((side * 4) * (i * 2 + 1), direction, color, inactiveColor, isStatic));
            }
            TransitionListener listener;
            Add(listener = new TransitionListener());
            listener.OnOutBegin = () =>
            {
                var player = Scene.Tracker.GetEntity<Player>();
                if (player != null)
                    TriggerExitAll(player);
            };
        }

        public override void Awake(Scene scene)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                foreach (var beam in Components.GetAll<Beam>())
                    beam.Enabled = (invertFlag != SceneAs<Level>().Session.GetFlag(flag));
            }

            base.Awake(scene);

            triggers = new List<Trigger>();
            exitTriggers = new List<Trigger>();
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var trigger = scene.CollideFirst<Trigger>(node);
                if (trigger == null)
                    trigger = scene.Tracker.GetNearestEntity<Trigger>(node);
                var list = exitNodes.Contains(i + 1) ? exitTriggers : triggers;
                if (trigger != null && !list.Contains(trigger))
                {
                    list.Add(trigger);
                    trigger.Collidable = false;
                }
            }
        }

        public override void Update()
        {
            if (!string.IsNullOrEmpty(flag))
            {
                foreach (var beam in Components.GetAll<Beam>())
                    beam.Enabled = (invertFlag != SceneAs<Level>().Session.GetFlag(flag));
            }

            base.Update();

            Beam bestBeam = null;
            var hasPlayer = false;
            foreach (var beam in Components.GetAll<Beam>())
            {
                if (beam.HasPlayer)
                    hasPlayer = true;

                if (bestBeam == null)
                    bestBeam = beam;
                else if (bestBeam.Length < beam.Length)
                    bestBeam = beam;
            }

            var rect = bestBeam?.GetRectangle();
            FixTriggers(triggers, rect);
            FixTriggers(exitTriggers, rect);

            Player player = Scene.Tracker.GetEntity<Player>();
            if (hasPlayer)
            {
                if (!hadPlayer)
                    TriggerEnter(player);
                TriggerStay(player);
            }
            else if (player != null)
            {
                if (hadPlayer)
                    TriggerLeave(player);
                if (exitAlwaysActive)
                    TriggerStayExit(player);
            }

            hadPlayer = hasPlayer;
        }

        private bool IsRiding(Solid solid) => CollideCheck(solid, Position + (direction.Perpendicular().Abs() * (size / 2f)) - direction);
        private bool IsRiding(JumpThru jumpThru) => CollideCheckOutside(jumpThru, Position + (direction.Perpendicular().Abs() * (size / 2f)) - direction);

        private void FixTriggers(List<Trigger> list, Rectangle? rect)
        {
            List<Trigger> toRemove = new List<Trigger>();
            foreach (var trigger in list)
            {
                if (trigger.Scene == null)
                    toRemove.Add(trigger);
                else if (rect != null)
                {
                    trigger.Position = direction.X != 0f ? new Vector2(rect.Value.X, Y) : new Vector2(X, rect.Value.Y);
                    trigger.Collider.Width = direction.X != 0f ? rect.Value.Width : size;
                    trigger.Collider.Height = direction.Y != 0f ? rect.Value.Height : size;
                }
            }
            foreach (var trigger in toRemove)
                list.Remove(trigger);
        }

        public void TriggerEnter(Player player)
        {
            foreach (var trigger in exitTriggers)
            {
                trigger.Triggered = false;
                if (trigger.PlayerIsInside)
                    trigger.OnLeave(player);
            }
            foreach (var trigger in triggers)
            {
                trigger.Triggered = true;
                if (!trigger.PlayerIsInside)
                    trigger.OnEnter(player);
            }
        }

        public void TriggerStay(Player player)
        {
            foreach (var trigger in triggers)
                trigger.OnStay(player);
        }

        public void TriggerLeave(Player player)
        {
            foreach (var trigger in triggers)
            {
                trigger.Triggered = false;
                if (trigger.PlayerIsInside)
                    trigger.OnLeave(player);
            }
            foreach (var trigger in exitTriggers)
            {
                trigger.Triggered = true;
                if (!trigger.PlayerIsInside)
                    trigger.OnEnter(player);
            }
        }

        public void TriggerStayExit(Player player)
        {
            foreach (var trigger in exitTriggers)
            {
                trigger.Triggered = true;
                if (!trigger.PlayerIsInside)
                    trigger.OnEnter(player);
                trigger.OnStay(player);
            }
        }

        public void TriggerExitAll(Player player)
        {
            foreach (var trigger in triggers)
            {
                trigger.Triggered = false;
                if (trigger.PlayerIsInside)
                    trigger.OnLeave(player);
            }
            foreach (var trigger in exitTriggers)
            {
                trigger.Triggered = false;
                if (trigger.PlayerIsInside)
                    trigger.OnLeave(player);
            }
        }

        public static void Load()
        {
            On.Celeste.Player.Removed += Player_Removed;
        }

        public static void Initialize()
        {
            TriggerBeam.P_Beam = new ParticleType
            {
                Source = GFX.Game["particles/confetti"],
                Color = Color.White * 0.5f,
                FadeMode = ParticleType.FadeModes.InAndOut,
                Size = 1f,
                SpeedMin = 32f,
                SpeedMax = 40f,
                LifeMin = 0.6f,
                LifeMax = 0.8f,
                RotationMode = ParticleType.RotationModes.SameAsDirection
            };
        }

        public static void Unload()
        {
            On.Celeste.Player.Removed -= Player_Removed;
        }

        private static void Player_Removed(On.Celeste.Player.orig_Removed orig, Player self, Scene scene)
        {
            orig(self, scene);
            if (scene != null)
                foreach (TriggerBeam beam in scene.Tracker.GetEntities<TriggerBeam>())
                    beam.TriggerExitAll(self);
        }

        private static Dictionary<string, Vector2> Directions = new Dictionary<string, Vector2>
        {
            { "Up", -Vector2.UnitY },
            { "Down", Vector2.UnitY },
            { "Right", Vector2.UnitX },
            { "Left", -Vector2.UnitX }
        };

        public class Beam : Component
        {
            public Vector2 RelativePosition;
            public Vector2 Direction;
            public Color Color;
            public Color InactiveColor;
            public bool IsStatic;

            public float Length;
            public bool Enabled;
            public bool HasPlayer;

            public Vector2 Position => Entity.Position + RelativePosition;

            private float? tileLength;

            public Beam(Vector2 pos, Vector2 dir, Color color, Color inactiveColor, bool isStatic) : base(true, true)
            {
                Enabled = true;
                Length = 0f;

                RelativePosition = pos;
                Direction = dir;
                Color = color;
                InactiveColor = inactiveColor;
                IsStatic = isStatic;
            }

            public override void EntityAwake()
            {
                FixLength();
            }

            public override void Update()
            {
                FixLength();
                var rect = GetRectangle();

                HasPlayer = Enabled && Scene.CollideFirst<Player>(rect) != null;

                if (Length > 0f && Scene.OnInterval(0.25f))
                {
                    Vector2 offset = new Vector2(TriggerBeam.P_Beam.Source.Width / 2f, TriggerBeam.P_Beam.Source.Height / 2f);
                    if (Direction.X > 0f || Direction.Y > 0f)
                        offset += new Vector2(-Direction.Y, -Direction.X);
                    offset *= Direction.Perpendicular();
                    var baseColor = Enabled ? Color : InactiveColor;
                    var particleColor = Color.Lerp(baseColor, Color.White * baseColor.A, 0.25f);
                    SceneAs<Level>().Particles.Emit(TriggerBeam.P_Beam, (int)Math.Ceiling(Length / 32f), new Vector2(rect.X + (rect.Width / 2f), rect.Y + (rect.Height / 2f)) + offset, new Vector2(rect.Width / 2f, rect.Height / 2f), particleColor, Direction.Angle());
                }
            }

            public override void Render()
            {
                if (Length > 0f)
                    Draw.Rect(GetRectangle(), Enabled ? Color : InactiveColor);
            }

            public Rectangle GetRectangle()
            {
                var perp = Direction.Perpendicular().Abs();
                var pos1 = Position - (perp * 4f);
                var pos2 = Position + (perp * 4f) + (Direction * Length);

                int x1 = (int)Math.Min(pos1.X, pos2.X);
                int y1 = (int)Math.Min(pos1.Y, pos2.Y);
                int x2 = (int)Math.Max(pos1.X, pos2.X);
                int y2 = (int)Math.Max(pos1.Y, pos2.Y);

                return new Rectangle(x1, y1, x2 - x1, y2 - y1);
            }

            private void FixLength()
            {
                var startPoint = Position + (Direction * Vector2.One);
                var solids = Scene.CollideAll<Solid>(startPoint, Position + (Direction * Math.Max(0f, MaxLength)));
                Length = MaxLength;
                foreach (var solid in solids)
                {
                    var newLength = 0f;
                    if (solid != null)
                    {
                        if (solid is SolidTiles)
                        {
                            if (tileLength == null || !IsStatic)
                            {
                                var length = 0;
                                while (length < MaxLength)
                                {
                                    if (solid.CollidePoint(startPoint + (Direction * length)))
                                    {
                                        newLength = length + 1;
                                        tileLength = newLength;
                                        break;
                                    }
                                    length++;
                                }
                            }
                            newLength = tileLength.Value;
                        }
                        else
                        {
                            if (Direction == Vector2.UnitX)
                                newLength = solid.Left - Position.X;
                            else if (Direction == -Vector2.UnitX)
                                newLength = Position.X - solid.Right;
                            else if (Direction == Vector2.UnitY)
                                newLength = solid.Top - Position.Y;
                            else if (Direction == -Vector2.UnitY)
                                newLength = Position.Y - solid.Bottom;
                        }
                    }
                    Length = Math.Min(Length, newLength);
                }
            }

            public float MaxLength
            {
                get
                {
                    if (Direction == Vector2.UnitX)
                        return SceneAs<Level>().Bounds.Right - Position.X;
                    else if (Direction == -Vector2.UnitX)
                        return Position.X - SceneAs<Level>().Bounds.Left;
                    else if (Direction == Vector2.UnitY)
                        return SceneAs<Level>().Bounds.Bottom - Position.Y;
                    else if (Direction == -Vector2.UnitY)
                        return Position.Y - SceneAs<Level>().Bounds.Top;
                    return 0f;
                }
            }
        }
    }
}
