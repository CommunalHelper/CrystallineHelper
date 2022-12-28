using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace vitmod
{
    [CustomEntity("vitellary/fastbounceblock")]
    public class FastBounceBlock : Solid
    {
        public FastBounceBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
        {
            blockType = data.Attr("blockType", "Core");
            reformed = true;
            state = States.Waiting;
            startPos = Position;
            hotImages = BuildSprite(GFX.Game["objects/bumpblocknew/fire00"]);
            hotCenterSprite = GFX.SpriteBank.Create("fastBumpBlockCenterFire");
            hotCenterSprite.Position = new Vector2(Width, Height) / 2f;
            hotCenterSprite.Visible = false;
            Add(hotCenterSprite);
            coldImages = BuildSprite(GFX.Game["objects/bumpblocknew/ice00"]);
            coldCenterSprite = GFX.SpriteBank.Create("fastBumpBlockCenterIce");
            coldCenterSprite.Position = new Vector2(Width, Height) / 2f;
            coldCenterSprite.Visible = false;
            Add(coldCenterSprite);
            Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            iceModeNext = iceMode = (blockType == "Core" && SceneAs<Level>().CoreMode == Session.CoreModes.Cold) || blockType == "Ice";
            ToggleSprite();
        }

        public override void Update()
        {
            base.Update();
            reappearFlash = Calc.Approach(reappearFlash, 0f, Engine.DeltaTime * 8f);
            switch (state)
            {
                case States.Waiting:
                    CheckModeChange();
                    moveSpeed = Calc.Approach(moveSpeed, 100f, 400f * Engine.DeltaTime);
                    Vector2 newPos = Calc.Approach(ExactPosition, startPos, moveSpeed * Engine.DeltaTime);
                    Vector2 liftSpeed = (newPos - ExactPosition).SafeNormalize(moveSpeed);
                    MoveTo(newPos, liftSpeed);
                    windUpProgress = Calc.Approach(windUpProgress, 0f, 1f * Engine.DeltaTime);
                    Player player = WindUpPlayerCheck();
                    if (player != null)
                    {
                        moveSpeed = 80f;
                        windUpStartTimer = 0f;
                        if (iceMode)
                        {
                            bounceDir = -Vector2.UnitY;
                            StartShaking(0.2f);
                            Audio.Play("event:/game/09_core/iceblock_touch", Center);
                        }
                        else
                        {
                            bounceDir = (player.Center - Center).SafeNormalize();
                            Audio.Play("event:/game/09_core/bounceblock_touch", Center);
                        }
                        state = States.WindingUp;
                        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    }
                    break;
                case States.WindingUp:
                    Player player2 = WindUpPlayerCheck();
                    if (player2 != null)
                    {
                        if (iceMode)
                        {
                            Break();
                            break;
                        }
                        else
                        {
                            bounceDir = (player2.Center - Center).SafeNormalize();
                        }
                    }
                    if (windUpStartTimer > 0f)
                    {
                        windUpStartTimer -= Engine.DeltaTime;
                        windUpProgress = Calc.Approach(windUpProgress, 0f, 1f * Engine.DeltaTime);
                    }
                    else
                    {
                        moveSpeed = Calc.Approach(moveSpeed, 80f, 1200f * Engine.DeltaTime);
                        Vector2 nextpos = startPos - bounceDir * 10f;
                        Vector2 currentpos = Calc.Approach(ExactPosition, nextpos, moveSpeed * Engine.DeltaTime);
                        Vector2 liftSpeed2 = (currentpos - ExactPosition).SafeNormalize(moveSpeed);
                        MoveTo(currentpos, liftSpeed2);
                        windUpProgress = Calc.ClampedMap(Vector2.Distance(ExactPosition, nextpos), 16f, 2f, 0f, 1f);
                        if (windUpProgress >= 0.5f)
                        {
                            StartShaking(0.1f);
                        }
                        if (Vector2.DistanceSquared(ExactPosition, nextpos) <= 2f)
                        {
                            state = States.Bouncing;
                            moveSpeed = 0f;
                        }
                    }
                    break;
                case States.Bouncing:
                    moveSpeed = Calc.Approach(moveSpeed, 280f, 1600f * Engine.DeltaTime);
                    Vector2 nextpos2 = startPos + bounceDir * 24f;
                    Vector2 currentpos2 = Calc.Approach(ExactPosition, nextpos2, moveSpeed * Engine.DeltaTime);
                    bounceLift = (currentpos2 - ExactPosition).SafeNormalize(Math.Min(moveSpeed * 3f, 200f));
                    bounceLift.X *= 0.75f;
                    MoveTo(currentpos2, bounceLift);
                    windUpProgress = 1f;
                    if (ExactPosition == nextpos2 || (WindUpPlayerCheck() == null))
                    {
                        debrisDir = (nextpos2 - startPos).SafeNormalize();
                        state = States.BounceEnd;
                        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                        moveSpeed = 0f;
                        bounceEndTimer = 0.05f;
                        ShakeOffPlayer(bounceLift);
                    }
                    break;
                case States.BounceEnd:
                    bounceEndTimer -= Engine.DeltaTime;
                    if (bounceEndTimer <= 0f)
                    {
                        Break();
                    }
                    break;
                case States.Broken:
                    Depth = 8990;
                    reformed = false;
                    if (respawnTimer > 0f)
                    {
                        respawnTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        Vector2 position = Position;
                        Position = startPos;
                        if (!CollideCheck<Actor>() && !CollideCheck<Solid>())
                        {
                            CheckModeChange();
                            Audio.Play(iceMode ? "event:/game/09_core/iceblock_reappear" : "event:/game/09_core/bounceblock_reappear", Center);
                            for (int i = 0; i < Width; i += 8)
                            {
                                for (int j = 0; j < Height; j += 8)
                                {
                                    Vector2 particlePos = new Vector2(X + i + 4f, Y + j + 4f);
                                    Scene.Add(Engine.Pooler.Create<RespawnDebris>().Init(particlePos + (particlePos - Center).SafeNormalize() * 12f, particlePos, iceMode, 0.35f));
                                }
                            }
                            Alarm.Set(this, 0.35f, delegate
                            {
                                reformed = true;
                                reappearFlash = 0.6f;
                                EnableStaticMovers();
                                ReformParticles();
                            }, Alarm.AlarmMode.Oneshot);
                            Depth = -9000;
                            MoveStaticMovers(this.Position - position);
                            Collidable = true;
                            state = States.Waiting;
                        }
                        else
                        {
                            Position = position;
                        }
                    }
                    break;
            }
        }

        public override void Render()
        {
            Vector2 position = Position;
            Position += Shake;
            if (state != States.Broken && reformed)
            {
                base.Render();
            }
            if (reappearFlash > 0f)
            {
                float num = Ease.CubeOut(reappearFlash);
                float num2 = num * 2f;
                Draw.Rect(X - num2, Y - num2, Width + num2 * 2f, Height + num2 * 2f, Color.White * num);
            }
            Position = position;
        }

        private void ToggleSprite()
        {
            hotCenterSprite.Visible = !iceMode;
            coldCenterSprite.Visible = iceMode;
            foreach (Image image in hotImages)
            {
                image.Visible = !iceMode;
            }
            foreach (Image image2 in coldImages)
            {
                image2.Visible = iceMode;
            }
        }

        private void CheckModeChange()
        {
            if (iceModeNext != iceMode)
            {
                iceMode = iceModeNext;
                ToggleSprite();
            }
        }

        private Player WindUpPlayerCheck()
        {
            Player player = CollideFirst<Player>(Position - Vector2.UnitY);
            if (player != null && player.Speed.Y < 0f)
            {
                player = null;
            }
            if (player == null)
            {
                player = CollideFirst<Player>(Position + Vector2.UnitX);
                if (!(player != null && player.StateMachine.State == 1 && player.Facing == Facings.Left))
                {
                    player = CollideFirst<Player>(Position - Vector2.UnitX);
                    if (!(player != null && player.StateMachine.State == 1 && player.Facing == Facings.Right))
                    {
                        player = null;
                    }
                }
            }
            return player;
        }

        private void Break()
        {
            if (!iceMode)
            {
                Audio.Play("event:/game/09_core/bounceblock_break", Center);
            }
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            state = States.Broken;
            Collidable = false;
            DisableStaticMovers();
            respawnTimer = 1.6f;
            Vector2 direction = new Vector2(0f, 1f);
            if (!iceMode)
            {
                direction = debrisDir;
            }
            for (int i = 0; i < Width; i += 8)
            {
                for (int j = 0; j < Height; j += 8)
                {
                    if (iceMode)
                    {
                        direction = (new Vector2(X + i + 4f, Y + j + 4f) - Center).SafeNormalize();
                    }
                    Scene.Add(Engine.Pooler.Create<BreakDebris>().Init(new Vector2(X + i + 4f, Y + j + 4f), direction, iceMode));
                }
            }
            Level level = SceneAs<Level>();
            for (int i = 0; i < Width; i += 4)
            {
                for (int j = 0; j < Height; j += 4)
                {
                    Vector2 vector = Position + new Vector2(2 + i, 2 + j) + Calc.Random.Range(-Vector2.One, Vector2.One);
                    float direction2 = iceMode ? (vector - Center).Angle() : debrisDir.Angle();
                    level.Particles.Emit(iceMode ? BounceBlock.P_IceBreak : BounceBlock.P_FireBreak, vector, direction2);
                }
            }
        }

        private void ShakeOffPlayer(Vector2 liftSpeed)
        {
            Player player = WindUpPlayerCheck();
            if (player != null)
            {
                player.StateMachine.State = 0;
                player.Speed = liftSpeed;
                player.StartJumpGraceTime();
            }
        }

        private void ReformParticles()
        {
            Level level = SceneAs<Level>();
            for (int i = 0; i < Width; i += 4)
            {
                level.Particles.Emit(BounceBlock.P_Reform, new Vector2(X + 2f + i + Calc.Random.Range(-1, 1), Y), -1.57079637f);
                level.Particles.Emit(BounceBlock.P_Reform, new Vector2(X + 2f + i + Calc.Random.Range(-1, 1), Bottom - 1f), 1.57079637f);
            }
            for (int j = 0; j < Height; j += 4)
            {
                level.Particles.Emit(BounceBlock.P_Reform, new Vector2(X, Y + 2f + j + Calc.Random.Range(-1, 1)), 3.14159274f);
                level.Particles.Emit(BounceBlock.P_Reform, new Vector2(Right - 1f, Y + 2f + j + Calc.Random.Range(-1, 1)), 0f);
            }
        }

        private void OnChangeMode(Session.CoreModes coreMode)
        {
            iceModeNext = coreMode == Session.CoreModes.Cold;
        }

        private List<Image> BuildSprite(MTexture source)
		{
			List<Image> list = new List<Image>();
            for (int i = 0; i < Width; i += 8)
            {
                for (int j = 0; j < Height; j += 8)
                {
                    int ix;
                    if (i == 0)
                    {
                        ix = 0;
                    }
                    else if (i >= Width - 8)
                    {
                        ix = source.Width / 8 - 1;
                    }
                    else
                    {
                        ix = Calc.Random.Next(1, source.Width / 8 - 1);
                    }
                    int iy;
                    if (j == 0)
                    {
                        iy = 0;
                    }
                    else if (j >= Height - 8)
                    {
                        iy = source.Height / 8 - 1;
                    }
                    else
                    {
                        iy = Calc.Random.Next(1, source.Height / 8 - 1);
                    }
                    Image image = new Image(source.GetSubtexture(ix * 8, iy * 8, 8, 8));
                    image.Position = new Vector2(i, j);
                    list.Add(image);
                    Add(image);
                }
            }
            return list;
		}

        private class BreakDebris : Entity
        {
            public BreakDebris Init(Vector2 position, Vector2 direction, bool ice)
            {
                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(ice ? "objects/bumpblocknew/ice_rubble" : "objects/bumpblocknew/fire_rubble");
                MTexture texture = Calc.Random.Choose(atlasSubtextures);
                if (sprite == null)
                {
                    Add(sprite = new Image(texture));
                    sprite.CenterOrigin();
                }
                else
                {
                    sprite.Texture = texture;
                }
                Position = position;
                direction = Calc.AngleToVector(direction.Angle() + Calc.Random.Range(-0.1f, 0.1f), 1f);
                speed = direction * (ice ? Calc.Random.Range(20, 40) : Calc.Random.Range(120, 200));
                duration = Calc.Random.Range(2, 3);
                return this;
            }

            public override void Update()
            {
                base.Update();
                if (percent >= 1f)
                {
                    RemoveSelf();
                }
                else
                {
                    Position += speed * Engine.DeltaTime;
                    speed.X = Calc.Approach(speed.X, 0f, 180f * Engine.DeltaTime);
                    speed.Y = speed.Y + 200f * Engine.DeltaTime;
                    percent += Engine.DeltaTime / duration;
                    sprite.Color = Color.White * (1f - percent);
                }
            }

            public override void Render()
            {
                sprite.DrawOutline(Color.Black, 1);
                base.Render();
            }

            private Image sprite;

            private Vector2 speed;

            private float percent;

            private float duration;
        }

        private class RespawnDebris : Entity
        {
            public RespawnDebris Init(Vector2 from, Vector2 to, bool ice, float duration)
            {
                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(ice ? "objects/bumpblocknew/ice_rubble" : "objects/bumpblocknew/fire_rubble");
                MTexture texture = Calc.Random.Choose(atlasSubtextures);
                if (sprite == null)
                {
                    Add(sprite = new Image(texture));
                    sprite.CenterOrigin();
                }
                else
                {
                    sprite.Texture = texture;
                }
                this.from = from;
                Position = from;
                percent = 0f;
                this.to = to;
                this.duration = duration;
                return this;
            }

            public override void Update()
            {
                if (percent >= 1f)
                {
                    RemoveSelf();
                }
                else
                {
                    percent += Engine.DeltaTime / duration;
                    Position = Vector2.Lerp(from, to, Ease.CubeIn(percent));
                    sprite.Color = Color.White * percent;
                }
            }

            public override void Render()
            {
                sprite.DrawOutline(Color.Black, 1);
                base.Render();
            }

            private Image sprite;

            private Vector2 from;

            private Vector2 to;

            private float percent;

            private float duration;
        }

		private enum States
        {
            Waiting,
            WindingUp,
            Bouncing,
            BounceEnd,
            Broken
        }

        private string blockType;

        private bool reformed;

        private States state;

        private Vector2 startPos;

        private List<Image> hotImages;

        private Sprite hotCenterSprite;

        private List<Image> coldImages;

        private Sprite coldCenterSprite;

        private bool iceMode;

        private bool iceModeNext;

        private float reappearFlash;

        private float moveSpeed;

        private float windUpProgress;

        private float windUpStartTimer;

        private float bounceEndTimer;

        private float respawnTimer;

        private Vector2 bounceDir;

        private Vector2 bounceLift;

        private Vector2 debrisDir;
    }
}
