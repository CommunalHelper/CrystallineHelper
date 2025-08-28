using Celeste;
using Celeste.Mod.Entities;
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
    [CustomEntity("vitellary/timecrystal")]
    public class TimeCrystal : Entity
    {
        public static void Load()
        {
            On.Celeste.CoreModeToggle.OnPlayer += CoreModeToggle_OnPlayer;
            On.Celeste.Player.WallBoosterCheck += Player_WallBoosterCheck;
            On.Celeste.LightningRenderer.Update += LightningRenderer_Update;
        }

        public static void Unload()
        {
            On.Celeste.CoreModeToggle.OnPlayer -= CoreModeToggle_OnPlayer;
            On.Celeste.Player.WallBoosterCheck -= Player_WallBoosterCheck;
            On.Celeste.LightningRenderer.Update -= LightningRenderer_Update;
        }

        public TimeCrystal(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            oneUse = data.Bool("oneUse", false);
            stopLength = data.Float("stopLength", 2f);
            useTime = data.Float("respawnTime", 2.5f);
            immediate = data.Bool("immediate", false);
            untilDash = data.Bool("untilDash", false);
            entityTypesIgnore = data.Attr("entityTypesToIgnore", "").Split(',');
            privateTimeScale = data.Float("timeScale", 0f);

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            if (!oneUse)
            {
                Add(outline = new Image(GFX.Game["objects/crystals/time/outline"]));
                outline.CenterOrigin();
                outline.Visible = false;
            }
            string spritetype = oneUse ? "idlenr" : "idle";
            Add(sprite = new Sprite(GFX.Game, "objects/crystals/time/" + (untilDash ? "untildash/" : "") + spritetype));
            sprite.AddLoop(spritetype, "", 0.1f);
            sprite.Play(spritetype, false, false);
            sprite.CenterOrigin();
            Add(flash = new Sprite(GFX.Game, "objects/crystals/time/flash"));
            flash.Add("flash", "", 0.05f);
            flash.OnFinish = delegate (string anim)
            {
                flash.Visible = false;
            };
            flash.CenterOrigin();
            Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
            }, false, false));
            Add(new MirrorReflection());
            Add(bloom = new BloomPoint(0.8f, 16f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));
            Add(sine = new SineWave(0.6f));
            sine.Randomize();
            UpdateY();
            Depth = -100;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
            else if (Scene.OnInterval(0.1f) && stopStage != 1)
            {
                level.ParticlesFG.Emit(untilDash ? P_Glow_UntilDash : P_Glow, 1, Position, Vector2.One * 5f);
            }
            UpdateY();
            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
            if (Scene.OnInterval(2f) && sprite.Visible)
            {
                flash.Play("flash", true, false);
                flash.Visible = true;
            }
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                sprite.Visible = true;
                outline.Visible = false;
                Depth = -100;
                wiggler.Start();
                Audio.Play("event:/game/general/diamond_return", Position);
                level.ParticlesFG.Emit(untilDash ? P_Regen_UntilDash : P_Regen, 16, Position, Vector2.One * 2f);
            }
        }

        private void UpdateY()
        {
            flash.Y = (sprite.Y = (bloom.Y = sine.Value * 2f));
        }

        public override void Render()
        {
            if (sprite.Visible)
            {
                sprite.DrawOutline(1);
            }
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(RefillRoutine(player), true));
            if (oneUse)
            {
                RemoveSelf();
            }
            else
            {
                respawnTimer = useTime;
            }
            entitiesToIgnore = entityTypesIgnore;
            if (stopStage != 1)
            {
                if (untilDash)
                {
                    if (player.Dashes < player.MaxDashes)
                    {
                        player.Dashes = player.MaxDashes;
                    }
                    VitModule.timeStopType = freezeTypes.UntilDash;
                }
                else
                {
                    stopTimer = stopLength;
                }
                timeScaleToSet = privateTimeScale;
                VitModule.timeStopScaleTimer = immediate ? 1f : 0f;
                stopStage = 1;
            }
            else
            {
                stopTimer += stopLength;
            }
        }

        private IEnumerator RefillRoutine(Player player)
        {
            Celeste.Celeste.Freeze(0.05f);
            yield return null;
            level.Shake(0.3f);
            sprite.Visible = (flash.Visible = false);
            if (!oneUse)
            {
                outline.Visible = true;
            }
            Depth = 8999;
            yield return 0.05f;
            float num = player.Speed.Angle();
            level.ParticlesFG.Emit(untilDash ? P_Shatter_UntilDash : P_Shatter, 5, Position, Vector2.One * 4f, num - 1.57079637f);
            level.ParticlesFG.Emit(untilDash ? P_Shatter_UntilDash : P_Shatter, 5, Position, Vector2.One * 4f, num + 1.57079637f);
            SlashFx.Burst(Position, num);
            yield break;
        }

        private static void CoreModeToggle_OnPlayer(On.Celeste.CoreModeToggle.orig_OnPlayer orig, CoreModeToggle self, Player player)
        {
            if (stopStage == 1) // if time is frozen, delay the activation by 1 frame so that it won't trigger until after time is normal
            {
                self.Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () =>
                {
                    orig(self, player);
                }, 0.01f, true));
            }
            else
            {
                orig(self, player);
            }
        }

        private static WallBooster Player_WallBoosterCheck(On.Celeste.Player.orig_WallBoosterCheck orig, Player self)
        {
            if (stopStage != 1)
            {
                return orig(self);
            }
            else
            {
                return null;
            }
        }

        private static void LightningRenderer_Update(On.Celeste.LightningRenderer.orig_Update orig, LightningRenderer self) {
            if (stopStage == 1 && !entitiesToIgnore.Contains("Celeste.LightningRenderer")) {
                if (self.dirty)
                    self.RebuildEdges();
                self.ToggleEdges(true); // updates edges not rendering when "offcamera"
                self.Get<CustomBloom>().Update(); // updates rectangle center not rendering when "offcamera"
                // Lightning Render is updated by default exempting Lightning from the update rules
            } else {
                orig(self);
            }
        }

        private bool oneUse;

        private float stopLength;

        private float useTime;

        private bool immediate;

        private bool untilDash;

        private string[] entityTypesIgnore;

        private float privateTimeScale;

        public static string[] entitiesToIgnore = new string[0];

        public static float timeScaleToSet = 0f;

        public static ParticleType P_Shatter;

        public static ParticleType P_Shatter_UntilDash;

        public static ParticleType P_Regen;

        public static ParticleType P_Regen_UntilDash;

        public static ParticleType P_Glow;

        public static ParticleType P_Glow_UntilDash;

        public static float stopTimer;

        public static float prevTimer;

        public static int stopStage;

        public static int prevStage;
        
        private Sprite sprite;

        private Sprite flash;

        private Image outline;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Level level;

        private SineWave sine;

        private float respawnTimer;

        public enum freezeTypes
        {
            Timer,
            UntilDash
        }
    }
}