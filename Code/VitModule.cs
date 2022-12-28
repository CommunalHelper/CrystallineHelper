using Celeste;
using Celeste.Mod;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

namespace vitmod
{
    public class VitModule : EverestModule
    {
        public static VitModule Instance;
        public VitModule()
        {
            Instance = this;
        }
        public class VitModuleSession : EverestModuleSession
        {
            public List<EntityID> doorsToReset = new List<EntityID>();
            public Dictionary<EntityID, EntityData> doorEntityDatas = new Dictionary<EntityID, EntityData>();
            public Dictionary<EntityID, Vector2> doorOffsets = new Dictionary<EntityID, Vector2>();

            public List<EntityID> keysToReset = new List<EntityID>();
            public Dictionary<EntityID, EntityData> keyEntityDatas = new Dictionary<EntityID, EntityData>();
            public Dictionary<EntityID, Vector2> keyOffsets = new Dictionary<EntityID, Vector2>();

            public List<EntityID> keyberriesToReset = new List<EntityID>();
            public Dictionary<EntityID, EntityData> keyberryEntityDatas = new Dictionary<EntityID, EntityData>();
            public Dictionary<EntityID, Vector2> keyberryPositions = new Dictionary<EntityID, Vector2>();
            public Dictionary<EntityID, Vector2> keyberryOffsets = new Dictionary<EntityID, Vector2>();
            public Dictionary<EntityID, bool> keyberryBubbles = new Dictionary<EntityID, bool>();

            public List<EntityID> keyIcesToReset = new List<EntityID>();
            public Dictionary<EntityID, EntityData> keyIceEntityDatas = new Dictionary<EntityID, EntityData>();
            public Dictionary<EntityID, Vector2> keyIceOffsets = new Dictionary<EntityID, Vector2>();
            public Dictionary<EntityID, Vector2[]> keyIceNodes = new Dictionary<EntityID, Vector2[]>();

            public List<EntityID> introCarsToReset = new List<EntityID>();
            public Dictionary<EntityID, EntityData> introCarEntityDatas = new Dictionary<EntityID, EntityData>();
            public Dictionary<EntityID, Vector2> introCarOffsets = new Dictionary<EntityID, Vector2>();

            public bool BombTimerActive;
            public Vector2 BombTimerStartPos;
            public string BombTimerStartLevel;
            public HashSet<EntityID> BombTimerStartKeys = new HashSet<EntityID>();
            public HashSet<EntityID> BombTimerRemovedEntities = new HashSet<EntityID>();
        }

        public class VitModuleSettings : EverestModuleSettings
        {
            public TriggerTrigger.RandomizationTypes TriggerTriggerRandomizationType
            { 
                get;
                set;
            } = TriggerTrigger.RandomizationTypes.FileTimer;
        }

        public override Type SessionType => typeof(VitModuleSession);
        public static VitModuleSession Session => (VitModuleSession)Instance._Session;

        public override Type SettingsType => typeof(VitModuleSettings);
        public static VitModuleSettings Settings => (VitModuleSettings)Instance._Settings;

        public override void LoadContent(bool firstLoad)
        {
            //fill crystal
            FillCrystal.P_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("ffddc2"),
                Color2 = Calc.HexToColor("fcad85")
            };
            FillCrystal.P_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("ffb6a3"),
                Color2 = Calc.HexToColor("ffb36b")
            };
            FillCrystal.P_Regen = new ParticleType(FillCrystal.P_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };

            //star crystal
            StarCrystal.P_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Source = GFX.Game["particles/stars/02"],
                Color = Calc.HexToColor("fff7a3"),
                Color2 = Calc.HexToColor("ffdb7d")
            };
            StarCrystal.P_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("ffec8c"),
                Color2 = Calc.HexToColor("fffba6")
            };
            StarCrystal.P_Regen = new ParticleType(StarCrystal.P_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };

            //tele crystal
            TeleCrystal.P_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("a6b5ff"),
                Color2 = Calc.HexToColor("cfd8ff")
            };
            TeleCrystal.P_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("cfecff"),
                Color2 = Calc.HexToColor("dbdeff")
            };
            TeleCrystal.P_Regen = new ParticleType(TeleCrystal.P_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };

            //time crystal
            TimeCrystal.P_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("bababa"),
                Color2 = Calc.HexToColor("dedede")
            };
            TimeCrystal.P_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("bababa"),
                Color2 = Calc.HexToColor("dedede")
            };
            TimeCrystal.P_Regen = new ParticleType(TimeCrystal.P_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };
            TimeCrystal.P_Shatter_UntilDash = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("aaddff"),
                Color2 = Calc.HexToColor("99aaff")
            };
            TimeCrystal.P_Glow_UntilDash = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("aaddff"),
                Color2 = Calc.HexToColor("99aaff")
            };
            TimeCrystal.P_Regen_UntilDash = new ParticleType(TimeCrystal.P_Glow_UntilDash)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };

            //force dash crystal
            ForceDashCrystal.P_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("ffccdd"),
                Color2 = Calc.HexToColor("eebbee")
            };
            ForceDashCrystal.P_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("ffccdd"),
                Color2 = Calc.HexToColor("eebbee")
            };
            ForceDashCrystal.P_Regen = new ParticleType(ForceDashCrystal.P_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };
            ForceDashCrystal.P_NeedDash_Shatter = new ParticleType(Refill.P_Shatter)
            {
                Color = Calc.HexToColor("cceeff"),
                Color2 = Calc.HexToColor("bbeeee")
            };
            ForceDashCrystal.P_NeedDash_Glow = new ParticleType(Refill.P_Glow)
            {
                Color = Calc.HexToColor("cceeff"),
                Color2 = Calc.HexToColor("bbeeee")
            };
            ForceDashCrystal.P_NeedDash_Regen = new ParticleType(ForceDashCrystal.P_NeedDash_Glow)
            {
                SpeedMin = 30f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.2f,
                DirectionRange = 6.28318548f
            };

            //force jump crystal (i'm just stealing from tele crystal it's basically the same colors)
            ForceJumpCrystal.P_Shatter = new ParticleType(TeleCrystal.P_Shatter);
            ForceJumpCrystal.P_Glow = new ParticleType(TeleCrystal.P_Glow);
            ForceJumpCrystal.P_Regen = new ParticleType(TeleCrystal.P_Regen);

            //linked move block
            VitMoveBlock.P_Activate = new ParticleType
            {
                Size = 1f,
                Color = Color.Black,
                FadeMode = ParticleType.FadeModes.Late,
                DirectionRange = 0.34906584f,
                LifeMin = 0.4f,
                LifeMax = 0.6f,
                SpeedMin = 20f,
                SpeedMax = 40f,
                SpeedMultiplier = 0.25f
            };
            VitMoveBlock.P_Break = new ParticleType(VitMoveBlock.P_Activate);
            VitMoveBlock.P_Move = new ParticleType(VitMoveBlock.P_Activate);

            //reset door trigger (respawning key)
            ResetDoorTrigger.P_Respawn = new ParticleType(Refill.P_Shatter)
            {
                Source = GFX.Game["particles/rect"],
                Color = Calc.HexToColor("e2d926"),
                Color2 = Calc.HexToColor("fffeef"),
                DirectionRange = 6.28318531f
            };
            ResetDoorTrigger.P_IceRespawn = new ParticleType(ResetDoorTrigger.P_Respawn)
            {
                Color = Calc.HexToColor("6385ff"),
                Color2 = Calc.HexToColor("72f0ff")
            };

            //boost bumper
            BoostBumper.P_Appear = new ParticleType(Booster.P_Appear)
            {
                Color = Calc.HexToColor("C0796A")
            };
            BoostBumper.P_Idle = new ParticleType(Bumper.P_Ambience);
        }

        public override void Initialize() {
            base.Initialize();
            TriggerBeam.Initialize();
        }
        public override void Load()
        {
            //cacheing
            deltaTimeInfo = typeof(Engine).GetProperty("DeltaTime");
            rawDeltaTimeInfo = typeof(Engine).GetProperty("RawDeltaTime");
            rendererListSceneInfo = typeof(RendererList).GetField("scene", BindingFlags.NonPublic | BindingFlags.Instance);
            frostHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata
            {
                Name = "FrostHelper",
                Version = new Version(1, 3, 0)
            });
            vivHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata
            {
                Name = "VivHelper",
                Version = new Version(1, 5, 4)
            });

            NoJumpTrigger.Load();
            NoDashTrigger.Load();
            NoGrabTrigger.Load();
            StarCrystal.Load();
            ForceDashCrystal.Load();
            NoMoveTrigger.Load();
            ResetDoorTrigger.Load();
            if (frostHelperLoaded)
            {
                HookedKeyIceInit();
            }
            CustomWindController.Load();
            TriggerTrigger.Load();
            TimeCrystal.Load();
            TimeFadeTrigger.Load();
            KaizoBlock.Load();

            BombTimerTrigger.Load();
            CustomPuffer.Load();
            DeadlyDashSwitch.Load();
            DropHoldableTrigger.Load();
            EnergyBooster.Load();
            FlagCrystal.Load();
            InteractiveChaser.Load();
            PairedDashSwitch.Load();
            TempleGateAllSwitches.Load();
            TriggerBeam.Load();


            //timestuff
            On.Celeste.Level.Update += Level_Update;
            IL.Monocle.EntityList.Update += EntityList_Update;
            IL.Monocle.RendererList.Update += RendererList_Update;
            On.Celeste.CrystalStaticSpinner.UpdateHue += CrystalStaticSpinner_UpdateHue;

            //player hooks
            On.Celeste.Player.Update += Player_Update;
            hookedPlayerUpdate = new ILHook(typeof(Player).GetMethod("orig_Update"), Player_IL_Update);
            On.Celeste.Player.Die += Player_Die;
            On.Celeste.Player.CallDashEvents += Player_CallDashEvents;
            On.Celeste.PlayerHair.GetHairColor += PlayerHair_GetHairColor;

            //effects
            Everest.Events.Level.OnLoadBackdrop += Level_OnLoadBackdrop;

            //misc
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Level.Reload += Level_Reload;
            On.Celeste.Level.End += Level_End;
        }

        private static void HookedKeyIceInit()
        {
            hookedKeyIceCtor = new Hook(typeof(FrostHelper.KeyIce).GetConstructor(new Type[]{typeof(EntityData), typeof(Vector2), typeof(EntityID), typeof(Vector2[])}), typeof(ResetDoorTrigger).GetMethod("KeyIce_ctor", BindingFlags.NonPublic | BindingFlags.Static));
            hookedKeyIceUpdate = new Hook(typeof(FrostHelper.KeyIce).GetMethod("Update"), typeof(ResetDoorTrigger).GetMethod("KeyIce_Update", BindingFlags.NonPublic | BindingFlags.Static));
            hookedKeyIceDissolveRoutine = new Hook(typeof(FrostHelper.KeyIce).GetMethod("DissolveRoutine", BindingFlags.NonPublic | BindingFlags.Instance), typeof(ResetDoorTrigger).GetMethod("KeyIce_DissolveRoutine", BindingFlags.NonPublic | BindingFlags.Static));
        }

        private void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            //timestop crystal
            if (!self.Paused)
            {
                if (TimeCrystal.stopTimer > 0f)
                {
                    TimeCrystal.stopTimer -= Engine.DeltaTime;
                    if (timeStopType == TimeCrystal.freezeTypes.Timer)
                    {
                        if (TimeCrystal.stopTimer <= 0f)
                        {
                            if (TimeCrystal.stopStage == 1)
                            {
                                TimeCrystal.stopTimer = 2f;
                                TimeCrystal.stopStage = 2;
                                timeStopScaleTimer = TimeCrystal.timeScaleToSet;
                            }
                            else if (TimeCrystal.stopStage == 2)
                            {
                                TimeCrystal.stopTimer = 0f;
                                TimeCrystal.stopStage = 0;
                                timeStopScaleTimer = 0f;
                            }
                        }
                    }
                }
                if (timeStopType == TimeCrystal.freezeTypes.UntilDash)
                {
                    if (TimeCrystal.stopTimer > 0f)
                    {
                        TimeCrystal.stopTimer -= Engine.DeltaTime;
                    }
                    if (TimeCrystal.stopTimer <= 0f)
                    {
                        Player player = self.Tracker.GetEntity<Player>();
                        if (player != null && player.Dashes < 1)
                        {
                            TimeCrystal.stopTimer = 2f;
                            TimeCrystal.stopStage = 2;
                            timeStopType = TimeCrystal.freezeTypes.Timer; //hacky thing to get it to resume time normally
                            timeStopScaleTimer = TimeCrystal.timeScaleToSet;
                        }
                    }
                }
            }

            if (TimeCrystal.stopStage > 0)
            {
                if (!self.Paused)
                {
                    if (TimeCrystal.timeScaleToSet < 1)
                    {
                        timeStopScaleTimer += Engine.DeltaTime;
                    }
                    else
                    {
                        timeStopScaleTimer -= Engine.DeltaTime;
                    }
                }

                float timestop_delta_mult = 1;
                if (TimeCrystal.stopStage == 1)
                {
                    timestop_delta_mult = Math.Max(TimeCrystal.timeScaleToSet, 1 - (timeStopScaleTimer / 0.5f));
                }
                else if (TimeCrystal.stopStage == 2)
                {
                    timestop_delta_mult = Math.Min(1, timeStopScaleTimer / 0.5f);
                }

                if (timestop_delta_mult != 1)
                {
                    useTimeStopDelta = true;
                    timeStopDelta = Engine.DeltaTime * timestop_delta_mult;
                    timeStopRawDelta = Engine.RawDeltaTime * timestop_delta_mult;
                }
                else
                {
                    useTimeStopDelta = false;
                }
            }
            else
            {
                useTimeStopDelta = false;
            }

            //no move trigger
            if (NoMoveTrigger.stopTimer > 0f && !self.Paused)
            {
                NoMoveTrigger.stopTimer -= Engine.DeltaTime;
                if (NoMoveTrigger.stopTimer <= 0f)
                {
                    if (NoMoveTrigger.stopStage == 1)
                    {
                        NoMoveTrigger.stopTimer = 2f;
                        NoMoveTrigger.stopStage = 2;
                    }
                    else if (NoMoveTrigger.stopStage == 2)
                    {
                        NoMoveTrigger.stopTimer = 0f;
                        NoMoveTrigger.stopStage = 0;
                    }
                    noMoveScaleTimer = 0f;
                }
            }

            if (NoMoveTrigger.stopStage > 0)
            {
                if (!self.Paused)
                {
                    noMoveScaleTimer += Engine.DeltaTime;
                }

                float nomove_delta_mult = NoMoveTrigger.GetDeltaMult();

                if (nomove_delta_mult != 1)
                {
                    useNoMoveDelta = true;
                    noMoveDelta = Engine.DeltaTime * nomove_delta_mult;
                }
                else
                {
                    useNoMoveDelta = false;
                }
            }
            else
            {
                useNoMoveDelta = false;
            }

            orig(self);
        }

        private void EntityList_Update(ILContext il) {
            var cursor = new ILCursor(il);

            int locEntity = 0;
            ILLabel labelEnd = null;
            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdloc(out locEntity),
                instr => instr.MatchLdfld<Entity>("Active")))
            {
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out labelEnd)))
                {
                    Logger.Log("CrystallineHelper", "Adding EntityList.Update hook");

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc, locEntity);
                    cursor.EmitDelegate<Action<EntityList, Entity>>((self, entity) => {
                        lastDeltaTime = Engine.DeltaTime;
                        lastRawDeltaTime = Engine.RawDeltaTime;

                        var timeStopCheck = useTimeStopDelta && !(entity is Player || entity is PlayerDeadBody
                            || entity is TimeCrystal || entity is CrystalStaticSpinner || entity is DustStaticSpinner
                            || entity is Lookout || entity is FakeWall);
                        if (frostHelperLoaded) {
                            timeStopCheck = timeStopCheck && !IsFrostHelperSpinner(entity);
                        }
                        if (vivHelperLoaded)
                        {
                            timeStopCheck = timeStopCheck && !IsVivHelperSpinner(entity);
                        }
                        var noMoveCheck = useNoMoveDelta && entity is Player;

                        if (entity.Scene is Level) {
                            if (noMoveCheck) {
                                deltaTimeInfo.SetValue(null, noMoveDelta);
                            } else if (timeStopCheck) {
                                if (!TimeCrystal.entitiesToIgnore.Contains(entity.GetType().FullName) &&
                                    !TimeCrystal.entitiesToIgnore.Contains(entity.GetType().Name) &&
                                    !(timeStopDelta < 0f && entity is ParticleSystem))
                                {
                                    deltaTimeInfo.SetValue(null, timeStopDelta);
                                    foreach (Component component in entity.Components)
                                    {
                                        if (component is SoundSource sound)
                                        {
                                            if (timeStopDelta == 0 && sound.Playing)
                                            {
                                                sound.Pause();
                                            }
                                            else if (timeStopDelta != 0 && !sound.Playing)
                                            {
                                                sound.Resume();
                                            }
                                        }
                                    }
                                    if (entity is ParticleSystem)
                                    {
                                        rawDeltaTimeInfo.SetValue(null, timeStopRawDelta);
                                    }
                                }
                            }
                        }
                    });

                    cursor.GotoLabel(labelEnd, MoveType.Before);

                    cursor.EmitDelegate<Action>(() => {
                        if (Engine.DeltaTime != lastDeltaTime) {
                            deltaTimeInfo.SetValue(null, lastDeltaTime);
                        }
                        if (Engine.RawDeltaTime != lastRawDeltaTime) {
                            rawDeltaTimeInfo.SetValue(null, lastRawDeltaTime);
                        }
                    });
                }
            }
        }

        private void RendererList_Update(ILContext il) {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<RendererList>("scene"),
                instr => instr.MatchCallvirt<Renderer>("Update")))
            {
                Logger.Log("CrystallineHelper", "Adding RendererList.Update hook");

                cursor.EmitDelegate<Func<Renderer, Renderer>>((renderer) => {
                    lastDeltaTime = Engine.DeltaTime;
                    if (useTimeStopDelta && !(renderer is DisplacementRenderer)) {
                        deltaTimeInfo.SetValue(null, timeStopDelta);
                    }
                    return renderer;
                });

                cursor.Index += 3;

                cursor.EmitDelegate<Action>(() => {
                    if (Engine.DeltaTime != lastDeltaTime) {
                        deltaTimeInfo.SetValue(null, lastDeltaTime);
                    }
                });
            }
        }

        private bool IsFrostHelperSpinner(Entity entity)
        {
            return entity is FrostHelper.CustomSpinner;
        }

        private bool IsVivHelperSpinner(Entity entity)
        {
            return entity is VivHelper.Entities.CustomSpinner || entity is VivHelper.Entities.AnimatedSpinner;
        }

        private void CrystalStaticSpinner_UpdateHue(On.Celeste.CrystalStaticSpinner.orig_UpdateHue orig, CrystalStaticSpinner self)
        {
            if (TimeCrystal.stopStage != 2)
            {
                orig(self);
            }
        }

        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            CustomBridge.bridgeList.Clear();
            orig(self, playerIntro, isFromLoader);
        }

        private void Level_Reload(On.Celeste.Level.orig_Reload orig, Level self)
        {
            orig(self);
            CustomWindController customWind = self.Entities.FindFirst<CustomWindController>();
            if (customWind != null)
            {
                customWind.SnapWind();
            }
            else
            {
                self.Wind = Vector2.Zero;
            }
        }

        private void Level_End(On.Celeste.Level.orig_End orig, Level self)
        {
            NoMoveTrigger.stopTimer = 0f;
            NoMoveTrigger.stopStage = 0;
            noMoveScaleTimer = 0f;
            NoMoveTrigger.alreadyIn = false;
            TimeCrystal.stopTimer = 0f;
            TimeCrystal.stopStage = 0;
            timeStopScaleTimer = 0f;
            orig(self);
        }

        private void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            orig(self);
            rainbowTimer += Engine.DeltaTime;
            if (StarCrystal.starDashTimer > 0f)
            {
                StarCrystal.starDashTimer -= Engine.DeltaTime;
            }
            if (StarCrystal.starInvulnTimer > 0f)
            {
                StarCrystal.starInvulnTimer -= Engine.DeltaTime;
                if (self.OnGround())
                {
                    self.RefillDash();
                }
            }
            if (StarCrystal.starStaminaTimer > 0f)
            {
                self.Stamina = 110f;
                StarCrystal.starStaminaTimer -= Engine.DeltaTime;
            }
        }

        private void Player_IL_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("ForceCameraUpdate")))
            {
                return;
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Engine>("get_DeltaTime")))
            {
                Logger.Log("CrystallineHelper", "Adding Player.Update IL hook");

                cursor.EmitDelegate<Func<float, float>>(dt =>
                {
                    if (useNoMoveDelta)
                    {
                        return lastDeltaTime;
                    }
                    else
                    {
                        return dt;
                    }
                });
            }
        }

        private void Player_CallDashEvents(On.Celeste.Player.orig_CallDashEvents orig, Player self)
        {
            foreach (BoostBumper booster in self.Scene.Tracker.GetEntities<BoostBumper>())
            {
                if (booster.startedBoosting)
                {
                    booster.PlayerBoosted(self, self.DashDir);
                    return;
                }
            }
            orig(self);
        }

        public static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            if (StarCrystal.starInvulnTimer > 0f && !evenIfInvincible)
            {
                return null;
            }
            PlayerDeadBody orig_result = orig(self, direction, evenIfInvincible, registerDeathInStats);
            if (orig_result != null)
            {
                //basically just reset all the variables for things
                NoMoveTrigger.stopTimer = 0f;
                NoMoveTrigger.stopStage = 0;
                noMoveScaleTimer = 0f;
                NoMoveTrigger.alreadyIn = false;
                TimeCrystal.stopTimer = 0f;
                TimeCrystal.stopStage = 0;
                timeStopScaleTimer = 0f;
                timeStopType = TimeCrystal.freezeTypes.Timer;
                StarCrystal.starDashTimer = 0f;
                StarCrystal.starInvulnTimer = 0f;
                StarCrystal.starStaminaTimer = 0f;
            }

            return orig_result;
        }

        private Color PlayerHair_GetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index)
        {
            Scene scene = self.Scene;
            if (scene == null)
            {
                return orig(self, index);
            }
            float startimer = Math.Max(Math.Max(StarCrystal.starDashTimer, StarCrystal.starInvulnTimer), StarCrystal.starStaminaTimer);
            Player player = scene.Tracker.GetEntity<Player>();
            if (player == null)
            {
                return orig(self, index);
            }
            if (debug)
            {
                return Calc.HexToColor("00ff00");
            }
            if (startimer > 0f)
            {
                float sat;
                if (player.Dashes == player.MaxDashes)
                {
                    sat = 1f;
                }
                else
                {
                    sat = 0.3f;
                }
                if (startimer < 0.2f)
                {
                    return Calc.HsvToColor((rainbowTimer + index * 0.1f) % 1f, 4.5f * startimer, sat);
                }
                else
                {
                    return Calc.HsvToColor((rainbowTimer + index * 0.1f) % 1f, 0.9f, sat);
                }
            }
            return orig(self, index);
        }

        private Backdrop Level_OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above)
        {
            Backdrop result;
            switch (child.Name)
            {
                case "CrystallineHelper/CustomWindSnow":
                    result = new CustomWindSnow(
                        child.Attr("colors", "ffffff"),
                        child.Attr("alphas", "1"),
                        child.AttrInt("amount", 240),
                        child.AttrFloat("speedX", 0f),
                        child.AttrFloat("speedY", 0f),
                        child.AttrBool("ignoreWind", false)
                    );
                    return result;
            }
            return null;
        }

        public static bool GetClassName(string name, Entity entity)
        {
            return entity.GetType().FullName == name || entity.GetType().Name == name;
        }

        public override void Unload()
        {
            NoJumpTrigger.Unload();
            NoDashTrigger.Unload();
            NoGrabTrigger.Unload();
            StarCrystal.Unload();
            ForceDashCrystal.Unload();
            NoMoveTrigger.Unload();
            ResetDoorTrigger.Unload();
            if (frostHelperLoaded)
            {
                HookedKeyIceInit();
            }
            CustomWindController.Unload();
            TriggerTrigger.Unload();
            TimeCrystal.Unload();
            TimeFadeTrigger.Unload();
            KaizoBlock.Unload();
            BombTimerTrigger.Unload();
            CustomPuffer.Unload();
            DeadlyDashSwitch.Unload();
            DropHoldableTrigger.Unload();
            EnergyBooster.Unload();
            FlagCrystal.Unload();
            InteractiveChaser.Unload();
            PairedDashSwitch.Unload();
            TempleGateAllSwitches.Unload();
            TriggerBeam.Unload();

            On.Celeste.Level.Update -= Level_Update;
            IL.Monocle.EntityList.Update -= EntityList_Update;
            IL.Monocle.RendererList.Update -= RendererList_Update;
            On.Celeste.CrystalStaticSpinner.UpdateHue -= CrystalStaticSpinner_UpdateHue;
            On.Celeste.Player.Update -= Player_Update;
            hookedPlayerUpdate.Dispose();
            On.Celeste.Player.Die -= Player_Die;
            On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
            On.Celeste.PlayerHair.GetHairColor -= PlayerHair_GetHairColor;
            Everest.Events.Level.OnLoadBackdrop -= Level_OnLoadBackdrop;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Level.Reload -= Level_Reload;
        }

        private float rainbowTimer;

        public static float timeStopScaleTimer;

        private bool useTimeStopDelta;

        private float timeStopDelta;

        private float timeStopRawDelta;

        public static TimeCrystal.freezeTypes timeStopType;

        public static Dictionary<string, Ease.Easer> EaseTypes = new Dictionary<string, Ease.Easer>
        {
            { "Linear", Ease.Linear },
            { "SineIn", Ease.SineIn },
            { "SineOut", Ease.SineOut },
            { "SineInOut", Ease.SineInOut },
            { "QuadIn", Ease.QuadIn },
            { "QuadOut", Ease.QuadOut },
            { "QuadInOut", Ease.QuadInOut },
            { "CubeIn", Ease.CubeIn },
            { "CubeOut", Ease.CubeOut },
            { "CubeInOut", Ease.CubeInOut },
            { "QuintIn", Ease.QuintIn },
            { "QuintOut", Ease.QuintOut },
            { "QuintInOut", Ease.QuintInOut },
            { "BackIn", Ease.BackIn },
            { "BackOut", Ease.BackOut },
            { "BackInOut", Ease.BackInOut },
            { "ExpoIn", Ease.ExpoIn },
            { "ExpoOut", Ease.ExpoOut },
            { "ExpoInOut", Ease.ExpoInOut },
            { "BigBackIn", Ease.BigBackIn },
            { "BigBackOut", Ease.BigBackOut },
            { "BigBackInOut", Ease.BigBackInOut },
            { "ElasticIn", Ease.ElasticIn },
            { "ElasticOut", Ease.ElasticOut },
            { "ElasticInOut", Ease.ElasticInOut },
            { "BounceIn", Ease.BounceIn },
            { "BounceOut", Ease.BounceOut },
            { "BounceInOut", Ease.BounceInOut }
        };

        public static float noMoveScaleTimer;

        private bool useNoMoveDelta;

        private float noMoveDelta;

        private float lastDeltaTime;

        private float lastRawDeltaTime;

        public static bool debug;

        private PropertyInfo deltaTimeInfo;

        private PropertyInfo rawDeltaTimeInfo;

        private FieldInfo rendererListSceneInfo;

        public static bool frostHelperLoaded;

        public static bool vivHelperLoaded;

        public static Hook hookedCustomSpinner;

        public static Hook hookedKeyIceCtor;
        public static Hook hookedKeyIceUpdate;
        public static Hook hookedKeyIceDissolveRoutine;
        public static ILHook hookedPlayerUpdate;
    }
}