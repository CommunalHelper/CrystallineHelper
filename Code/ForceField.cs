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
using Celeste.Mod;

namespace vitmod
{
    [CustomEntity("vitellary/forcefield")]
    public class ForceField : Entity
    {
        public ForceField(EntityData data, Vector2 offset, EntityID gid) : base(data.Position + offset)
        {
            texture = data.Attr("texture");
            tint = Calc.HexToColor(data.Attr("tint"));
            flag = data.Attr("flag");
            if (flag.StartsWith("!"))
            {
                flag = flag.Substring(1);
                invert = true;
            }
            visibleDist = data.Float("visibleDistance");
            canClip = data.Bool("allowClipping");
            silent = data.Bool("silent");

            nodes = data.NodesOffset(offset);
            rngOffset = Calc.Random.NextFloat();
            lastSigns = new int[nodes.Length];
            ends = new List<Sprite>();
            lasers = new List<List<Sprite>>();

            Depth = -8499; // below spinners, hopefully above a lot of other things
            Tag = Tags.TransitionUpdate;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Sprite startsprite = new Sprite(GFX.Game, "objects/" + texture + "/end");
            startsprite.AddLoop("idle", "", 0.1f);
            startsprite.Play("idle");
            startsprite.CenterOrigin();
            startsprite.Color = tint;
            Add(startsprite);
            ends.Add(startsprite);

            // if (!silent)
            Add(sound = new SoundSource("event:/new_content/env/10_electricity"));
            if (silent)
                sound.Stop();

            bounds = new Rectangle((int)X, (int)Y, 0, 0);
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector2 nodePos = nodes[i];
                if (nodePos.X < bounds.X) {
                    bounds.Width += (int)(bounds.X - nodePos.X);
                    bounds.X = (int)nodePos.X;
                } else {
                    bounds.Width = (int)Math.Max(bounds.Width, nodePos.X - bounds.X);
                }
                if (nodePos.Y < bounds.Y) {
                    bounds.Height += (int)(bounds.Y - nodePos.Y);
                    bounds.Y = (int)nodePos.Y;
                } else {
                    bounds.Height = (int)Math.Max(bounds.Height, nodePos.Y - bounds.Y);
                }

                Vector2 node = nodePos - Position;
                Sprite nodesprite = new Sprite(GFX.Game, "objects/" + texture + "/end");
                nodesprite.AddLoop("idle", "", 0.1f);
                nodesprite.Play("idle");
                nodesprite.CenterOrigin();
                nodesprite.Position = node;
                Add(nodesprite);
                ends.Add(nodesprite);

                Vector2 prev = Vector2.Zero;
                if (i > 0) prev = nodes[i - 1] - Position;
                float angle = Calc.Angle(node - prev);
                Vector2 start = prev + Calc.AngleToVector(angle, 4f);
                Vector2 end = node - Calc.AngleToVector(angle, 4f);
                float dist = Vector2.Distance(start, end);

                MTexture lasertexture = GFX.Game["objects/" + texture + "/laser00"];
                float size = dist / lasertexture.Width;
                int amount = (int)Math.Round(size);
                float scale = size / amount;
                List<Sprite> laser = new List<Sprite>();
                for (int j = 0; j < amount; j++)
                {
                    Sprite lasersprite = new Sprite(GFX.Game, "objects/" + texture + "/laser");
                    lasersprite.AddLoop("idle", "", 0.1f);
                    lasersprite.Play("idle", false, true);
                    lasersprite.SetOrigin(0f, lasersprite.Height / 2f);
                    lasersprite.Rotation = angle;
                    lasersprite.Scale.X = scale;
                    lasersprite.Position = start + Calc.AngleToVector(angle, j * lasertexture.Width * scale);
                    Add(lasersprite);
                    laser.Add(lasersprite);
                }
                lasers.Add(laser);
            }
        }

        public override void Update()
        {
            base.Update();
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player == null) return;

            if (Scene.OnInterval(0.25f, rngOffset) && !InView()) {
                inView = false;
            } else if (!inView && InView()) {
                inView = true;
            }

            // set alphas for each node
            if (inView) {
                if (visibleDist > 0) {
                    float dist = visibleDist * 2f;
                    for (int i = 0; i <= nodes.Length; i++) {
                        Vector2 node = Position;
                        if (i > 0)
                            node = nodes[i - 1];
                        dist = Math.Min(Vector2.Distance(player.Position, node), dist);
                    }
                    alpha = Calc.ClampedMap(dist, visibleDist, visibleDist * 2f, 1f, 0f);
                } else
                    alpha = 1f;
            } else {
                alpha = 0f;
            }

            if (flag != "")
            {
                Collidable = SceneAs<Level>().Session.GetFlag(flag);
                if (invert) Collidable = !Collidable;
            }

            if (Collidable)
            {
                if (!sound.Playing && !silent)
                    sound.Play("event:/new_content/env/10_electricity");

                for (int i = 0; i < nodes.Length; i++)
                {
                    Vector2 node = nodes[i];
                    Vector2 start = Position;
                    if (i > 0)
                    {
                        start = nodes[i - 1];
                    }
                    Vector2 offset = node - start;

                    if (player.CollideLine(start, node))
                    {
                        OnPlayer(player);
                        break;
                    }

                    if (!canClip)
                    {
                        // i don't know what any of this math is doing i stole it from gravity helper
                        float num = Vector2.Dot(player.Center - start, offset) / Vector2.Dot(offset, offset);
                        Vector2 vector = start + offset * num;
                        int num2 = Math.Sign((vector - player.Center).SafeNormalize().Angle());
                        if (offset.X == 0f)
                        {
                            num2 = (player.Center.X < start.X) ? -1 : 1;
                        }
                        if (num >= 0f && num <= 1f && lastSigns[i] != 0 && num2 != lastSigns[i])
                        {
                            OnPlayer(player);
                            break;
                        }
                        lastSigns[i] = num2;
                    }
                }
            }
            else
            {
                alpha = 0f;
                if (sound.Playing && !silent)
                    sound.Stop();
            }
        }

        private bool InView()
        {
            Camera camera = (Scene as Level).Camera;
            return bounds.Right > camera.Left - 16f
                   && bounds.Left < camera.Right + 16f
                   && bounds.Bottom > camera.Top - 16f
                   && bounds.Top < camera.Bottom + 16f;
        }

        public void OnPlayer(Player player)
        {
            player.Die(Vector2.Zero);
        }

        public override void Render()
        {
            if (alpha == 0f)
                return;

            foreach (Sprite nodesprite in ends)
            {
                nodesprite.Color = tint * alpha;
                nodesprite.DrawOutline(Color.Black * alpha);
            }
            foreach (List<Sprite> fullLaser in lasers)
            {
                foreach (Sprite laser in fullLaser)
                {
                    laser.Color = tint * alpha;
                    laser.DrawOutline(Color.Black * alpha);
                }
            }
            base.Render();
        }

        public override void DebugRender(Camera camera)
        {
            base.DebugRender(camera);
            Color color = Color.Red;
            if (!Collidable) color *= 0.5f;
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector2 node = nodes[i];
                Vector2 start = Position;
                if (i > 0)
                {
                    start = nodes[i - 1];
                }
                Draw.Line(start, node, color);
            }
            // Draw.HollowRect(bounds, Color.Cyan);
        }

        private string texture;
        private Color tint;
        private string flag;
        private bool invert;
        private Vector2[] nodes;
        private Rectangle bounds;
        private float rngOffset;
        private bool inView;
        private bool silent;

        private float visibleDist;
        private bool canClip;

        private int[] lastSigns;
        private float alpha;

        private List<Sprite> ends;
        private List<List<Sprite>> lasers;
        private SoundSource sound;
    }
}
