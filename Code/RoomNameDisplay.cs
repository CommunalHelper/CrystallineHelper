using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Celeste.Mod.Code.Entities
{
    [Tracked]
    public class RoomNameDisplay : Entity
    {
        private float drawLerp;
        private float textLerp;
        private string text;
        private string nextText;
        private float yOffset;
        public float nextOffset;

        public Color bgColor;
        public Color textColor;
        private Color lineColor;
        private float colorLerp;
        private Color bgColorStart;
        private Color textColorStart;
        private Color lineColorStart;
        private float outline;
        private float nextOutline;

        public float scale;
        public float prevScale;
        public float nextScale;

        private float outTimer;
        public RoomNameDisplay() {
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
            Depth = -100;
            drawLerp = 0f;
            textLerp = 0f;
            colorLerp = 1f;
            outTimer = 0f;
            scale = 1f;
            prevScale = 1f;
            nextScale = 1f;

            bgColor = Color.Black;
            textColor = Color.White;

            var trans = new TransitionListener();
            trans.OnInEnd = () =>
            {
                if (Scene.Tracker.GetEntity<RoomNameController>() == null)
                {
                    SetName("");
                }
            };
            Add(trans);
        }

        public override void Update()
        {
            base.Update();
            float speed = 1.5f;
            if (nextText != "")
            {
                if (drawLerp > 0f || !(Scene as Level).Transitioning) {
                    drawLerp = Calc.Approach(drawLerp, 1f, Engine.DeltaTime * speed);
                }
            }
            else
            {
                drawLerp = Calc.Approach(drawLerp, 0f, Engine.DeltaTime * speed);
                if (drawLerp == 0f)
                {
                    RemoveSelf();
                }
            }

            if (text == nextText)
            {
                textLerp = Calc.Approach(textLerp, 1f, Engine.DeltaTime * speed);
                scale = Calc.LerpClamp(prevScale, nextScale, Ease.CubeOut(textLerp));
            }
            else
            {
                textLerp = Calc.Approach(textLerp, 0f, Engine.DeltaTime * speed);
                if (textLerp == 0f)
                {
                    text = nextText;
                    outline = nextOutline;
                    yOffset = nextOffset;
                }
            }

            if (colorLerp < 1f)
            {
                colorLerp = Calc.Approach(colorLerp, 1f, Engine.DeltaTime * speed);
            }

            if (outTimer > 0f)
            {
                outTimer = Calc.Approach(outTimer, 0f, Engine.DeltaTime);
                if (outTimer == 0f)
                {
                    SetName("");
                }
            }
        }

        public override void Render()
        {
            base.Render();
            var y = Calc.LerpClamp(1080f, 1080f - (48f * scale) + yOffset, Ease.CubeOut(drawLerp));
            Color bgC = bgColor;
            if (colorLerp < 1f)
            {
                bgC = Color.Lerp(bgColorStart, bgColor, colorLerp);
            }
            Draw.Rect(-2f, y, 1920f + 4f, (1080f - y) + 2f, bgC);
            if (text != "")
            {
                Color textC = textColor;
                if (colorLerp < 1f) {
                    textC = Color.Lerp(textColorStart, textColor, colorLerp);
                }
                var texty = Calc.LerpClamp(1080f, 1080f - (48f * scale) + yOffset, Ease.CubeOut(Calc.Min(textLerp, drawLerp)));
                if (outline > 0f)
                {
                    ActiveFont.DrawOutline(text, new Vector2(960, texty - 6f), new Vector2(0.5f, 0f), new Vector2(scale, scale), textC, outline, lineColor);
                }
                else
                {
                    ActiveFont.Draw(text, new Vector2(960, texty - 6f), new Vector2(0.5f, 0f), new Vector2(scale, scale), textC);
                }
            }
        }

        public void SetName(string name)
        {
            if (name != "" && Dialog.Has(name))
            {
                nextText = Dialog.Clean(name);
            }
            else
            {
                nextText = name;
            }
        }

        public void SetColor(string textHex, string bgHex, string lineHex, float amt)
        {
            Color text = HexToColor(textHex);
            Color bg = HexToColor(bgHex);
            Color line = HexToColor(lineHex);

            if (colorLerp < 1f)
            {
                bgColorStart = Color.Lerp(bgColorStart, bgColor, colorLerp);
                textColorStart = Color.Lerp(textColorStart, textColor, colorLerp);
                lineColorStart = Color.Lerp(lineColorStart, lineColor, colorLerp);
            }
            else
            {
                bgColorStart = bgColor;
                textColorStart = textColor;
                lineColorStart = lineColor;
            }
            colorLerp = 0f;
            if (drawLerp == 0f)
            {
                colorLerp = 1f;
            }
            bgColor = bg;
            textColor = text;
            lineColor = line;
            nextOutline = amt;
        }

        // Calc.HexToColorWithAlpha does not work correctly
        // need to do it myself
        public Color HexToColor(string hex)
        {
            if (hex[0] == '#')
            {
                hex = hex.Substring(1);
            }
            Color color = Calc.HexToColor(hex);
            if (hex.Length == 6)
            {
                return color;
            }
            float alpha = (Calc.HexToByte(hex[6]) * 16 + Calc.HexToByte(hex[7])) / 255f;
            return color * alpha;
        }

        public void SetTimer(float timer)
        {
            outTimer = timer;
        }

        public void SetInstant()
        {
            if (nextText == "")
            {
                RemoveSelf();
            }
            else
            {
                text = nextText;
                drawLerp = 1f;
                colorLerp = 1f;
                textLerp = 1f;
                scale = nextScale;
            }
        }

        public static RoomNameDisplay GetDisplay(Scene scene)
        {
            RoomNameDisplay display = scene.Tracker.GetEntity<RoomNameDisplay>();
            if (display == null)
            {
                display = new RoomNameDisplay();
                scene.Add(display);
            }
            return display;
        }
    }
}
