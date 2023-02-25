using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace vitmod
{
    [CustomEntity("vitellary/forcedashcrystal")]
    public class ForceDashCrystal : Entity
    {
        public static void Load()
        {
            On.Celeste.Input.GetAimVector += Input_GetAimVector;
        }

        public static void Unload()
        {
            On.Celeste.Input.GetAimVector -= Input_GetAimVector;
        }

        public ForceDashCrystal(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            direction = data.Attr("direction", "Right");
            oneUse = data.Bool("oneUse", false);
            useTime = data.Float("respawnTime", 2.5f);
            needDash = data.Bool("needDash", false);

            string crystalType = needDash ? "needdash" : "dashless";
            string useType = oneUse ? "idlenr" : "idle";

            sprites = new List<Sprite>();
            spritePositions = new List<Vector2>();
            flashes = new List<Sprite>();
            sines = new List<SineWave>();

            switch (direction)
            {
                case "Downright":
                    dirVector = Vector2.One;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)Math.PI * 0.5f;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)Math.PI * 0.5f;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(2f, 2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(1f, -3f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-3f, 1f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/diag/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)Math.PI * 0.5f;
                    outline.Visible = false;
                    break;
                case "Down":
                    dirVector = Vector2.UnitY;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)Math.PI;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)Math.PI;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(0, 2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(4f, -2f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-4f, -2f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/ortho/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)Math.PI;
                    outline.Visible = false;
                    break;
                case "Downleft":
                    dirVector = new Vector2(-1f, 1f);
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)Math.PI;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)Math.PI;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(-2f, 2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(-1f, -3f);
                    flashes[2].Position = sprites[2].Position = new Vector2(3f, 1f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/diag/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)Math.PI;
                    outline.Visible = false;
                    break;
                case "Left":
                    dirVector = -Vector2.UnitX;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)-Math.PI * 0.5f;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)-Math.PI * 0.5f;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(-2f, 0);
                    flashes[1].Position = sprites[1].Position = new Vector2(2f, -4f);
                    flashes[2].Position = sprites[2].Position = new Vector2(2f, 4f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/ortho/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)-Math.PI * 0.5f;
                    outline.Visible = false;
                    break;
                case "Upleft":
                    dirVector = -Vector2.One;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)-Math.PI * 0.5f;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)-Math.PI * 0.5f;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(-2f, -2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(-1f, 3f);
                    flashes[2].Position = sprites[2].Position = new Vector2(3f, -1f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/diag/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)-Math.PI * 0.5f;
                    outline.Visible = false;
                    break;
                case "Up":
                    dirVector = -Vector2.UnitY;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(0, -2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(4f, 2f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-4f, 2f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/ortho/outline"]);
                    outline.CenterOrigin();
                    outline.Visible = false;
                    break;
                case "Upright":
                    dirVector = new Vector2(1f, -1f);
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(2f, -2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(1f, 3f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-3f, -1f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/diag/outline"]);
                    outline.CenterOrigin();
                    outline.Visible = false;
                    break;
                case "Right":
                    dirVector = Vector2.UnitX;
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = (float)-Math.PI * 0.5f;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/ortho/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = (float)-Math.PI * 0.5f;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(2f, 0);
                    flashes[1].Position = sprites[1].Position = new Vector2(-2f, -4f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-2f, 4f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/ortho/outline"]);
                    outline.CenterOrigin();
                    outline.Rotation = (float)Math.PI * 0.5f;
                    outline.Visible = false;
                    break;
                default: // None
                    for (int i = 0; i < 4; i++)
                    {
                        Sprite sprite = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/" + useType);
                        sprite.AddLoop("idle", "", 0.1f);
                        sprite.Play("idle", false, false);
                        sprite.CenterOrigin();
                        sprite.Rotation = i * (float)Math.PI / 2f;
                        Add(sprite);
                        sprites.Add(sprite);

                        Sprite flash = new Sprite(GFX.Game, "objects/crystals/forcedash/" + crystalType + "/diag/flash");
                        flash.Add("flash", "", 0.05f);
                        flash.OnFinish = delegate (string anim)
                        {
                            flash.Visible = false;
                        };
                        flash.CenterOrigin();
                        flash.Rotation = i * (float)Math.PI / 2f;
                        Add(flash);
                        flashes.Add(flash);
                    }
                    flashes[0].Position = sprites[0].Position = new Vector2(2f, -2f);
                    flashes[1].Position = sprites[1].Position = new Vector2(2f, 2f);
                    flashes[2].Position = sprites[2].Position = new Vector2(-2f, 2f);
                    flashes[3].Position = sprites[3].Position = new Vector2(-2f, -2f);

                    outline = new Image(GFX.Game["objects/crystals/forcedash/" + crystalType + "/diag/outline_none"]);
                    outline.CenterOrigin();
                    outline.Visible = false;
                    break;
            }
            Add(outline);

            for (int i = 0; i < (direction == "None" ? 4 : 3); i++)
            {
                spritePositions.Add(sprites[i].Position);
                sprites[i].SetAnimationFrame(Calc.Random.Next());

                SineWave sine = new SineWave(0.6f, Calc.Random.Range(0f, 1f));
                Add(sine);
                sines.Add(sine);
            }

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));

            Add(wiggler = Wiggler.Create(1f, 4f, (float v) =>
            {
                for(int i = 0; i < (direction == "None" ? 4 : 3); i++)
                {
                    flashes[i].Scale = sprites[i].Scale = Vector2.One * (1f + v * 0.2f);
                }
            }, false, false));
            Add(new MirrorReflection());
            Add(bloom = new BloomPoint(0.8f, 16f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));

            Depth = -100;
            UpdateSprite();
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
            else if (Scene.OnInterval(0.1f))
            {
                SceneAs<Level>().ParticlesFG.Emit(P_Glow, 1, Position, Vector2.One * 5f);
            }
            UpdateSprite();
            light.Alpha = Calc.Approach(light.Alpha, sprites[1].Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
            if (Scene.OnInterval(2f) && sprites[1].Visible)
            {
                foreach (Sprite flash in flashes)
                {
                    flash.Play("flash", true, false);
                    flash.Visible = true;
                }
            }
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            dirToUse = null;
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            dirToUse = null;
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                foreach (Sprite sprite in sprites)
                {
                    sprite.Visible = true;
                }
                outline.Visible = false;
                Depth = -100;
                wiggler.Start();
                Audio.Play("event:/game/general/diamond_return", Position);
                SceneAs<Level>().ParticlesFG.Emit(P_Regen, 16, Position, Vector2.One * 2f);
            }
        }

        private void UpdateSprite()
        {
            if (direction == "None")
            {
                for (int i = 0; i < 4; i++)
                {
                    flashes[i].Position = sprites[i].Position =
                        spritePositions[i] + (new Vector2(1f, -1f).Rotate(i * (float)Math.PI / 2f) * sines[i].Value);
                }
            }
            else
            {
                if (dirVector.X == 0 || dirVector.Y == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        flashes[i].Position = sprites[i].Position = spritePositions[i] + (dirVector * sines[i].Value * 2f);
                    }
                    bloom.Position = dirVector * sines[0].Value * 2f;
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        flashes[i].Position = sprites[i].Position = spritePositions[i] + (dirVector * sines[i].Value);
                    }
                    bloom.Position = dirVector * sines[0].Value;
                }
            }
        }

        public override void Render()
        {
            foreach (Sprite sprite in sprites)
            {
                if (sprite.Visible)
                {
                    sprite.DrawOutline(1);
                }
            }
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            if (!needDash || player.Dashes > 0)
            {
                player.StateMachine.State = 0;
                if (direction != "None")
                {
                    dirToUse = Vector2.Normalize(dirVector);
                }
                player.StateMachine.State = 2;
                if (needDash)
                {
                    player.Dashes--;
                }
                Audio.Play("event:/game/general/diamond_touch", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine(RefillRoutine(player), true));
                if (!oneUse)
                {
                    respawnTimer = useTime;
                }
            }
        }

        private IEnumerator RefillRoutine(Player player)
        {
            Celeste.Celeste.Freeze(0.05f);
            yield return null;
            Level level = SceneAs<Level>();
            level.Shake(0.3f);
            for (int i = 0; i < (direction == "None" ? 4 : 3); i++)
            {
                flashes[i].Visible = sprites[i].Visible = false;
            }
            if (!oneUse)
            {
                outline.Visible = true;
            }
            Depth = 8999;
            yield return 0.05f;
            if (dirToUse == Vector2.Normalize(dirVector))
            {
                dirToUse = null;
            }
            float num = player.Speed.Angle();
            level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, num - 1.57079637f);
            level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, num + 1.57079637f);
            SlashFx.Burst(Position, num);
            if (oneUse)
            {
                RemoveSelf();
            }
            yield break;
        }

        public static Vector2 Input_GetAimVector(On.Celeste.Input.orig_GetAimVector orig, Facings defaultFacing)
        {
            return dirToUse ?? orig(defaultFacing);
        }

        private bool oneUse;

        private string direction;

        private float useTime;

        private bool needDash;

        private Vector2 dirVector;

        public static Vector2? dirToUse;

        private float respawnTimer;

        private List<Sprite> sprites;

        private List<Sprite> flashes;

        private List<Vector2> spritePositions;

        private Image outline;

        private List<SineWave> sines;

        public static ParticleType P_Shatter;

        public static ParticleType P_Regen;

        public static ParticleType P_Glow;

        public static ParticleType P_NeedDash_Shatter;

        public static ParticleType P_NeedDash_Regen;

        public static ParticleType P_NeedDash_Glow;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private static MethodInfo playerDashBegin = typeof(Player).GetMethod("DashBegin", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
