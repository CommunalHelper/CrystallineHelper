using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace vitmod
{
	public class BombTimerDisplay : Entity
	{
		public float CompleteTimer = 0f;

		public const int GuiChapterHeight = 58;

		public const int GuiFileHeight = 78;

		private static float numberWidth = 0f;

		private static float spacerWidth = 0f;

		private MTexture bg = GFX.Gui["strawberryCountBG"];

		public float DrawLerp = 0f;

		private bool LastActive = false;

		private Wiggler wiggler;

		public BombTimerDisplay()
		{
			base.Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
			base.Depth = -100;
			base.Y = 60f;
			CalculateBaseSizes();
			Add(wiggler = Wiggler.Create(0.5f, 4f));
		}

		private void CalculateBaseSizes()
		{
			PixelFont font = Dialog.Languages["english"].Font;
			float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
			PixelFontSize pixelFontSize = font.Get(fontFaceSize);
			for (int i = 0; i < 10; i++)
			{
				float x = pixelFontSize.Measure(i.ToString()).X;
				if (x > numberWidth)
				{
					numberWidth = x;
				}
			}
			spacerWidth = pixelFontSize.Measure('.').X;
		}

		public override void Update()
		{
			if (BombTimerTrigger.IsActive && !Scene.Paused)
			{
				Player player = Scene.Tracker.GetEntity<Player>();
				if (player != null)
				{
					BombTimerTrigger.Timer -= Engine.DeltaTime;
					if (BombTimerTrigger.Timer <= 0f)
					{
						BombTimerTrigger.Timer = 0f;
						if (player != null && !SceneAs<Level>().Transitioning)
						{
							BombTimerTrigger.ResetOnDeath = true;
							player.Die(Vector2.Zero);
						}
					}
					if (BombTimerTrigger.SoundEffect != null && BombTimerTrigger.Timer <= BombTimerTrigger.SoundEffect.Item1)
					{
						Audio.Play(BombTimerTrigger.SoundEffect.Item2);
						BombTimerTrigger.SoundEffect = null;
					}
				}
			}

			if (LastActive && !BombTimerTrigger.IsActive)
			{
				wiggler.Start();
				CompleteTimer = 3f;
			}
			if (CompleteTimer > 0f)
			{
				CompleteTimer -= Engine.DeltaTime;
			}

			bool enabled = BombTimerTrigger.Peek || BombTimerTrigger.IsActive || CompleteTimer > 0f;
			DrawLerp = Calc.Approach(DrawLerp, enabled ? 1 : 0, Engine.DeltaTime * 4f);
			LastActive = BombTimerTrigger.IsActive;

			if (DrawLerp > 0f)
			{
				Y = Engine.Height - 60f - bg.Height;
			}

			base.Update();
		}

		public override void Render()
		{
			if (DrawLerp > 0f)
			{
				float num = -300f * Ease.CubeIn(1f - DrawLerp);
				string timeString = TimeSpan.FromSeconds(BombTimerTrigger.Timer).ShortGameplayFormat();
				bg.Draw(new Vector2(num, base.Y));
				DrawTime(new Vector2(num + 32f, base.Y + 44f), timeString, 1f + wiggler.Value * 0.15f);
			}
		}

		private void DrawTime(Vector2 position, string timeString, float scale = 1f, float alpha = 1f)
		{
			PixelFont font = Dialog.Languages["english"].Font;
			float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
			float num = scale;
			float num2 = position.X;
			float num3 = position.Y;
			Color color = Calc.HexToColor("FF5656") * alpha;
			Color color2 = Calc.HexToColor("FF2B2B") * alpha;
			if (!BombTimerTrigger.IsActive && BombTimerTrigger.Timer == 0f && !BombTimerTrigger.Peek)
			{
				color = Calc.HexToColor("C41717") * alpha;
				color2 = Calc.HexToColor("7A0E0E") * alpha;
			}
			else if (!BombTimerTrigger.IsActive && BombTimerTrigger.Timer > 0f && !BombTimerTrigger.Peek)
			{
				color = Calc.HexToColor("6ded87") * alpha;
				color2 = Calc.HexToColor("43d14c") * alpha;
			}
			for (int i = 0; i < timeString.Length; i++)
			{
				char c = timeString[i];
				if (c == '.')
				{
					num = scale * 0.7f;
					num3 -= 5f * scale;
				}
				Color color3 = ((c == ':' || c == '.' || num < scale) ? color2 : color);
				float num4 = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * num;
				font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(num2 + num4 / 2f, num3), new Vector2(0.5f, 1f), Vector2.One * num, color3, 2f, Color.Black);
				num2 += num4;
			}
		}

		private float GetTimeWidth(string timeString, float scale = 1f)
		{
			float num = scale;
			float num2 = 0f;
			foreach (char c in timeString)
			{
				if (c == '.')
				{
					num = scale * 0.7f;
				}
				float num3 = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * num;
				num2 += num3;
			}
			return num2;
		}
	}
}
