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
    [CustomEntity("vitellary/triggertrigger")]
    [Tracked(false)]
    public class TriggerTrigger : Trigger
    {
        public static void Load()
        {
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.PlayerCollider.Check += PlayerCollider_Check;
        }

        public static void Unload()
        {
            On.Celeste.Player.Jump -= Player_Jump;
            On.Celeste.PlayerCollider.Check -= PlayerCollider_Check;
        }

        public TriggerTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            nodes = data.NodesOffset(offset);
            oneUse = data.Bool("oneUse", false);
            activationType = data.Enum("activationType", ActivationTypes.Flag);
            if (data.Has("invertFlag"))
            {
                invertCondition = data.Bool("invertFlag", false);
            }
            else
            {
                invertCondition = data.Bool("invertCondition", false);
            }
            comparisonType = data.Enum("comparisonType", ComparisonTypes.EqualTo);
            absoluteValue = data.Bool("absoluteValue", false);

            flag = data.Attr("flag", "");
            deaths = data.Int("deaths", -1);
            dashCount = data.Int("dashCount", 0);
            requiredSpeed = data.Float("requiredSpeed", 0f);
            waitTime = data.Float("timeToWait", 0f);
            coreMode = data.Enum("coreMode", Session.CoreModes.None);
            if (string.IsNullOrEmpty(data.Attr("entityType", "")))
            {
                collideEnity = data.Attr("entityTypeToCollide", "Celeste.Strawberry");
            }
            else
            {
                collideEnity = data.Attr("entityType", "");
            }
            collideCount = data.Int("collideCount", 1);
            entitiesTouched = new List<Entity>();
            collideSolid = data.Attr("solidType", "");
            entitiesInside = new List<Entity>();
            Add(new HoldableCollider((Holdable holdable) => {
                if (activationType == ActivationTypes.OnHoldableEnter)
                {
                    entitiesInside.Add(holdable.Entity);
                }
            }));
            if (activationType == ActivationTypes.OnInteraction)
            {
                Add(new TalkComponent(
                    new Rectangle(0, 0, (int)Width, (int)Height),
                    new Vector2(data.Int("talkBubbleX", (int)Width / 2), data.Int("talkBubbleY", 0)),
                    (player) =>
                    {
                        externalActivation = true;
                    }
                )
                {
                    PlayerMustBeFacing = false
                });
            }

            delay = data.Float("delay", 0f);
            randomize = data.Bool("randomize", false);
            Global = data.Bool("activateOnTransition", false);
            if (data.Has("requirePlayerInside"))
            {
                Global = !data.Bool("requirePlayerInside", true);
            }
            if (bypassGlobal.Contains(activationType))
            {
                Global = true;
            }
            matchPosition = data.Bool("matchPosition", true);
            onlyOnEnter = data.Bool("onlyOnEnter", false);

            Add(new TransitionListener
            {
                OnOut = (f) => DeactivateTriggers(Scene?.Tracker.GetEntity<Player>())
            });
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            triggers = new List<Trigger>();
            foreach (Vector2 node in nodes)
            {
                var lastCollideable = new Dictionary<Trigger, bool>();
                foreach (Trigger trig in scene.Tracker.GetEntities<Trigger>())
                {
                    lastCollideable.Add(trig, trig.Collidable);
                    trig.Collidable = true;
                }

                var trigger = scene.CollideFirst<Trigger>(node);

                foreach (Trigger trig in scene.Tracker.GetEntities<Trigger>())
                {
                    trig.Collidable = lastCollideable[trig];
                }

                if (trigger == null)
                {
                    trigger = scene.Tracker.GetNearestEntity<Trigger>(node);
                }
                if (trigger != this && trigger != null)
                {
                    triggers.Add(trigger);
                    trigger.Collidable = false;
                }
            }
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (!Global)
            {
                TryActivate(player);
                if (Activated && oneUse)
                {
                    RemoveSelf();
                }
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            if (!Global)
            {
                TryDeactivate(player, false);
            }
        }

        public override void Update()
        {
            base.Update();

            var player = Scene.Tracker.GetEntity<Player>();

            if (player == null) { return; }

            UpdateConditions(player);

            if (!onlyOnEnter)
            {
                if (Global || PlayerIsInside)
                {
                    TryActivate(player);
                    TryDeactivate(player, true);
                }
                else
                {
                    TryDeactivate(player, false);
                }
            }

            if (Activated)
            {
                if (oneUse)
                {
                    RemoveSelf();
                }
                else
                {
                    UpdateTriggers(player);
                }
            }
        }

        public void UpdateConditions(Player player)
        {
            if (player.Speed == Vector2.Zero)
            {
                hasWaited += Engine.DeltaTime;
            }
            else
            {
                hasWaited = 0f;
            }

            List<Entity> entitiesToRemove = new List<Entity>();
            foreach (Entity entity in entitiesInside)
            {
                if (!entity.CollideCheck(this))
                {
                    entitiesToRemove.Add(entity);
                }
            }
            foreach (Entity entity in entitiesToRemove)
            {
                entitiesInside.Remove(entity);
            }
        }

        public void TryActivate(Player player)
        {
            if (activating || (Activated && !deactivating)) return;

            if (GetActivateCondition(player))
            {
                if (delay > 0f)
                {
                    activating = true;
                    Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () =>
                    {
                        activating = false;
                        ActivateTriggers(player);
                    }, delay, true));
                }
                else
                {
                    ActivateTriggers(player);
                }
            }
        }

        public void TryDeactivate(Player player, bool inside)
        {
            if (deactivating || (!Activated && !activating)) return;

            if (!inside || !GetActivateCondition(player))
            {
                if (delay > 0f)
                {
                    deactivating = true;
                    Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () =>
                    {
                        deactivating = false;
                        DeactivateTriggers(player);
                    }, delay, true));
                }
                else
                {
                    DeactivateTriggers(player);
                }
            }
        }

        public bool GetActivateCondition(Player player)
        {
            bool result = false;
            switch (activationType)
            {
                case ActivationTypes.Flag:
                    result = string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag);
                    break;
                case ActivationTypes.Dashing:
                    result = player.DashAttacking;
                    break;
                case ActivationTypes.DashCount:
                    result = Compare(player.Dashes, dashCount);
                    break;
                case ActivationTypes.DeathsInRoom:
                    result = Compare(SceneAs<Level>().Session.DeathsInCurrentLevel, deaths);
                    break;
                case ActivationTypes.DeathsInLevel:
                    result = Compare(SceneAs<Level>().Session.Deaths, deaths);
                    break;
                case ActivationTypes.OnHoldableEnter:
                    result = entitiesInside.Count > 0;
                    break;
                case ActivationTypes.GrabHoldable:
                    result = player.Holding != null && player.Holding.IsHeld;
                    break;
                case ActivationTypes.SpeedX:
                    result = Compare(player.Speed.X, requiredSpeed);
                    break;
                case ActivationTypes.SpeedY:
                    result = Compare(player.Speed.Y, requiredSpeed);
                    break;
                case ActivationTypes.Jumping:
                    result = player.AutoJumpTimer > 0f;
                    break;
                case ActivationTypes.Crouching:
                    result = player.Ducking;
                    break;
                case ActivationTypes.TimeSinceMovement:
                    result = Compare(hasWaited, waitTime);
                    break;
                case ActivationTypes.CoreMode:
                    result = player.SceneAs<Level>().CoreMode == coreMode;
                    break;
                case ActivationTypes.OnEntityCollide:
                    result = Compare(collideEntityCount, collideCount);
                    break;
                case ActivationTypes.OnSolid:
                    Rectangle playerCollision = player.Collider.Bounds;
                    playerCollision.Inflate(1, 3);
                    foreach (Solid solid in Scene.CollideAll<Solid>(playerCollision))
                    {
                        if (string.IsNullOrEmpty(collideSolid) || VitModule.GetClassName(collideSolid, solid))
                        {
                            if (player.IsRiding(solid))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(collideSolid) && player.OnGround())
                    {
                        result = true;
                    }
                    break;
                case ActivationTypes.OnEntityEnter:
                    foreach (Entity entity in Scene.Entities)
                    {
                        if (entity.CollideCheck(this) && VitModule.GetClassName(collideEnity, entity))
                        {
                            result = true;
                            break;
                        }
                    }
                    break;
                default:
                    result = false;
                    break;
            }
            if (externalActivation)
            {
                result = true;
            }
            if (invertCondition)
            {
                result = !result;
            }
            return result;
        }

        private bool Compare(float a, float b)
        {
            if (absoluteValue)
            {
                a = Math.Abs(a);
                b = Math.Abs(b);
            }
            switch (comparisonType)
            {
                case ComparisonTypes.LessThan:
                    return a < b;
                case ComparisonTypes.GreaterThan:
                    return a > b;
                default:
                    return a == b;
            }
        }

        private void CleanTriggers()
        {
            triggers.RemoveAll((trigger) => trigger.Scene == null);

            if (chosenTrigger?.Scene == null)
            {
                chosenTrigger = null;
            }
        }

        private void ActivateTriggers(Player player)
        {
            DeactivateTriggers(player);
            CleanTriggers();

            Activated = true;

            if (!randomize)
            {
                foreach (Trigger trigger in triggers)
                {
                    if (trigger.PlayerIsInside)
                    {
                        trigger.OnLeave(player);
                    }
                    trigger.OnEnter(player);
                }
            }
            else if (triggers.Count > 0)
            {
                if (VitModule.Settings.TriggerTriggerRandomizationType == RandomizationTypes.FileTimer)
                {
                    Calc.PushRandom((int)(SaveData.Instance.Time % int.MaxValue));
                }
                else
                {
                    Calc.PushRandom((int)(SceneAs<Level>().Session.Time % int.MaxValue));
                }
                chosenTrigger = Calc.Choose(Calc.Random, triggers);
                Calc.PopRandom();

                if (chosenTrigger.PlayerIsInside)
                {
                    chosenTrigger.OnLeave(player);
                }
                chosenTrigger.OnEnter(player);
            }
        }

        private void DeactivateTriggers(Player player)
        {
            CleanTriggers();

            Activated = false;

            foreach (Trigger trigger in triggers)
            {
                if (trigger.PlayerIsInside)
                {
                    trigger.OnLeave(player);
                }
            }

            chosenTrigger = null;
        }

        private void UpdateTriggers(Player player)
        {
            CleanTriggers();

            foreach (Trigger trigger in triggers)
            {
                if (matchPosition)
                {
                    if (!Global)
                    {
                        trigger.Position = Position;
                        trigger.Collider.Width = Width;
                        trigger.Collider.Height = Height;
                    }
                    else
                    {
                        var level = SceneAs<Level>();
                        trigger.Position = new Vector2(level.Bounds.X, level.Bounds.Y);
                        trigger.Collider.Width = level.Bounds.Width;
                        trigger.Collider.Height = level.Bounds.Height;
                    }
                }

                if (!randomize)
                {
                    trigger.OnStay(player);
                }
            }

            chosenTrigger?.OnStay(player);
        }

        private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
        {
            orig(self, particles, playSfx);
            if (self == null) { return; }
            foreach (TriggerTrigger trigger in self.SceneAs<Level>().Tracker.GetEntities<TriggerTrigger>())
            {
                if (trigger.activationType == ActivationTypes.Jumping)
                {
                    trigger.externalActivation = true;
                    self.Add(new Coroutine(trigger.JumpRoutine(self, trigger), true));
                }
            }
        }

        public IEnumerator JumpRoutine(Player player, TriggerTrigger trigger)
        {
            while (!player.OnGround())
            {
                yield return null;
            }
            trigger.externalActivation = false;
            yield break;
        }

        private static bool PlayerCollider_Check(On.Celeste.PlayerCollider.orig_Check orig, PlayerCollider self, Player player)
        {
            bool result = orig(self, player);

            Entity entity = self.Entity;
            foreach (TriggerTrigger trigger in self.SceneAs<Level>().Tracker.GetEntities<TriggerTrigger>())
            {
                if (result)
                {
                    if (trigger.activationType == ActivationTypes.OnEntityCollide && VitModule.GetClassName(trigger.collideEnity, entity))
                    {
                        if (!trigger.entitiesTouched.Contains(entity))
                        {
                            trigger.entitiesTouched.Add(entity);
                            trigger.collideEntityCount++;
                        }
                    }
                }
                else if (trigger.entitiesTouched.Contains(entity))
                {
                    trigger.entitiesTouched.Remove(entity);
                }
            }

            return result;
        }

        public bool Global;
        public bool Activated;

        private Vector2[] nodes;
        private bool oneUse;
        public ActivationTypes activationType;
        private string flag;
        private int deaths;
        private int dashCount;
        private float requiredSpeed;
        private float waitTime;
        private float hasWaited;
        public string collideEnity;
        public int collideEntityCount;
        private int collideCount;
        public List<Entity> entitiesTouched;
        private Session.CoreModes coreMode;
        private List<Entity> entitiesInside;
        private string collideSolid;
        public bool externalActivation;
        private bool invertCondition;
        private ComparisonTypes comparisonType;
        private bool absoluteValue;
        private float delay;
        private bool randomize;
        private bool matchPosition;
        private bool onlyOnEnter;

        private List<Trigger> triggers;
        private bool activating;
        private bool deactivating;
        private Trigger chosenTrigger;

        public enum ActivationTypes
        {
            Flag,
            Dashing,
            DashCount,
            DeathsInRoom,
            DeathsInLevel,
            GrabHoldable,
            SpeedX,
            SpeedY,
            Jumping,
            Crouching,
            TimeSinceMovement,
            OnHoldableEnter,
            CoreMode,
            OnEntityCollide,
            OnInteraction,
            OnSolid,
            OnEntityEnter,
        };
        public enum RandomizationTypes
        {
            FileTimer,
            ChapterTimer
        };
        private static List<ActivationTypes> bypassGlobal = new List<ActivationTypes>() {
            ActivationTypes.OnHoldableEnter,
            ActivationTypes.OnInteraction,
            ActivationTypes.OnEntityEnter,
        };
        private enum ComparisonTypes
        {
            LessThan,
            EqualTo,
            GreaterThan,
        };
    }
}
