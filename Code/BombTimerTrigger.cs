using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace vitmod
{
    [CustomEntity("vitellary/bombtimer")]
    public class BombTimerTrigger : Trigger
    {
        enum StartDirection
        {
            Any,
            Right,
            Left,
            Up,
            Down
        }

        public static float Timer;
        public static bool IsActive;
        public static bool Peek;
        public static bool ResetOnDeath;
        public static Tuple<float, string> SoundEffect;

        private EntityID ID;
        private float timer;
        private float soundAt;
        private string sound;
        private bool changeRespawn;
        private bool resetOnDeath;
        private StartDirection startDirection;
        public BombTimerTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            ID = new EntityID(data.Level.Name, data.ID);
            timer = data.Float("timer", 0f);
            soundAt = data.Float("soundAt", 0f);
            sound = data.Attr("sound", "");
            changeRespawn = data.Bool("changeRespawn", true);
            resetOnDeath = data.Bool("resetOnDeath", true);
            startDirection = data.Enum<StartDirection>("startDirection", StartDirection.Any);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if ((scene as Level).Session.DoNotLoad.Contains(ID))
                RemoveSelf();
        }

        public override void OnStay(Player player)
        {
            base.OnStay(player);
            if (timer > 0f && !IsActive)
            {
                Timer = timer;
                Peek = true;
            }
            bool moving = false;
            switch(startDirection)
            {
                case StartDirection.Any: moving = (player.Speed != Vector2.Zero); break;
                case StartDirection.Right: moving = (player.Speed.X > 0f); break;
                case StartDirection.Left: moving = (player.Speed.X < 0f); break;
                case StartDirection.Up: moving = (player.Speed.Y < 0f); break;
                case StartDirection.Down: moving = (player.Speed.Y > 0f); break;
            }
            if (moving && (timer > 0f || IsActive))
            {
                Session session = SceneAs<Level>().Session;
                if (changeRespawn)
                {
                    Vector2 target = SceneAs<Level>().GetSpawnPoint(Center);
                    if (!session.RespawnPoint.HasValue || session.RespawnPoint.Value != target)
                    {
                        session.HitCheckpoint = true;
                        session.RespawnPoint = target;
                        session.UpdateLevelStartDashes();
                    }
                }
                if (timer > 0f)
                {
                    VitModule.Session.BombTimerActive = true;
                    VitModule.Session.BombTimerStartPos = session.RespawnPoint.GetValueOrDefault();
                    VitModule.Session.BombTimerStartLevel = session.Level;
                    VitModule.Session.BombTimerStartKeys = new HashSet<EntityID>(session.Keys);
                    VitModule.Session.BombTimerRemovedEntities.Clear();
                    Timer = timer;
                    Peek = false;
                    ResetOnDeath = resetOnDeath;
                    IsActive = true;
                    SoundEffect = (sound != "") ? new Tuple<float, string>(soundAt, sound) : null;
                }
                else
                {
                    VitModule.Session.BombTimerActive = false;
                    IsActive = false;
                }
                RemoveSelf();
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            if (timer > 0f)
            {
                Peek = false;
            }
        }

        public static void Load()
        {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Player.Die += Player_Die;
            On.Celeste.LockBlock.UnlockRoutine += LockBlock_UnlockRoutine;
        }

        public static void Unload()
        {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Player.Die -= Player_Die;
            On.Celeste.LockBlock.UnlockRoutine -= LockBlock_UnlockRoutine;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            if (isFromLoader && VitModule.Session.BombTimerActive)
            {
                ResetSession(self.Session);
            }
            orig(self, playerIntro, isFromLoader);
            if (isFromLoader)
            {
                self.Add(new BombTimerDisplay());

                Timer = 0f;
                IsActive = false;
                ResetOnDeath = false;
                Peek = false;
            }
        }

        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            bool reset = IsActive && ResetOnDeath;
            if (reset)
            {
                Timer = 0f;
                IsActive = false;

                ResetSession(self.SceneAs<Level>().Session);
            }
            PlayerDeadBody body = orig(self, direction, evenIfInvincible, registerDeathInStats);
            if (reset && body != null)
            {
                body.HasGolden = true;
            }
            return body;
        }

        private static IEnumerator LockBlock_UnlockRoutine(On.Celeste.LockBlock.orig_UnlockRoutine orig, LockBlock self, Follower fol)
        {
            IEnumerator orig_enum = orig.Invoke(self, fol);
            bool doneThing = false;
            while (orig_enum.MoveNext())
            {
                if (!doneThing && self.UnlockingRegistered)
                {
                    doneThing = true;
                    VitModule.Session.BombTimerRemovedEntities.Add(self.ID);
                    EntityID keyId = (fol.Entity as Key).ID;
                    if (!VitModule.Session.BombTimerStartKeys.Contains(keyId))
                    {
                        VitModule.Session.BombTimerRemovedEntities.Add(keyId);
                    }
                }
                yield return orig_enum.Current;
            }
            yield break;
        }

        private static void ResetSession(Session session)
        {
            session.RespawnPoint = VitModule.Session.BombTimerStartPos;
            session.Level = VitModule.Session.BombTimerStartLevel;
            foreach (EntityID key in session.Keys)
            {
                if (!VitModule.Session.BombTimerStartKeys.Contains(key))
                {
                    session.DoNotLoad.Remove(key);
                }
            }
            session.Keys = new HashSet<EntityID>(VitModule.Session.BombTimerStartKeys);
            foreach (EntityID door in VitModule.Session.BombTimerRemovedEntities)
            {
                session.DoNotLoad.Remove(door);
            }
            VitModule.Session.BombTimerActive = false;
        }
    }
}
