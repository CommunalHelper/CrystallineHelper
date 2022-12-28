using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace vitmod
{
    public class CustomPlayerHair : Component
    {
		public const string Hair = "characters/player/hair00";

		public Color Color = Player.NormalHairColor;
		public Color Border = Color.Black;
		public float Alpha = 1f;
		public Facings Facing;
		public bool DrawPlayerSpriteOutline;
		public bool SimulateMotion = true;
		public Vector2 StepPerSegment = new Vector2(0f, 2f);
		public float StepInFacingPerSegment = 0.5f;
		public float StepApproach = 64f;
		public float StepYSinePerSegment = 0f;
		public PlayerSprite Sprite;
		public List<Vector2> Nodes = new List<Vector2>();
		public Vector2 Scale;

		private List<MTexture> bangs = GFX.Game.GetAtlasSubtextures("characters/player/bangs");
		private float wave = 0f;

		public float Wave => wave;

		public CustomPlayerHair(PlayerSprite sprite, Vector2 scale)
			: base(active: true, visible: true)
		{
			Sprite = sprite;
			Scale = scale;
			for (int i = 0; i < sprite.HairCount; i++)
			{
				Nodes.Add(Vector2.Zero);
			}
		}

		public void Start()
		{
			Vector2 value = base.Entity.Position + new Vector2((0 - Facing) * 200, 200f);
			for (int i = 0; i < Nodes.Count; i++)
			{
				Nodes[i] = value;
			}
		}

		public void AfterUpdate()
		{
			Vector2 value = (Sprite.HairOffset * new Vector2(1f, Scale.Y)) * new Vector2((float)Facing, 1f);
			Nodes[0] = Sprite.RenderPosition + new Vector2(0f, -9f * Sprite.Scale.Y) + value;
			Vector2 target = Nodes[0] + (new Vector2((float)(0 - Facing) * StepInFacingPerSegment * 2f, (float)Math.Sin(wave) * StepYSinePerSegment) + StepPerSegment) * Scale.Y;
			Vector2 vector = Nodes[0];
			float num = 3f;
			for (int i = 1; i < Sprite.HairCount; i++)
			{
				if (i >= Nodes.Count)
				{
					Nodes.Add(Nodes[i - 1]);
				}
				if (SimulateMotion)
				{
					float num2 = (1f - (float)i / (float)Sprite.HairCount * 0.5f) * StepApproach;
					Nodes[i] = Calc.Approach(Nodes[i], target, num2 * Engine.DeltaTime);
				}
				if ((Nodes[i] - vector).Length() > num)
				{
					Nodes[i] = vector + (Nodes[i] - vector).SafeNormalize() * num;
				}
				target = Nodes[i] + (new Vector2((float)(0 - Facing) * StepInFacingPerSegment, (float)Math.Sin(wave + (float)i * 0.8f) * StepYSinePerSegment) + StepPerSegment) * Scale.Y;
				vector = Nodes[i];
			}
		}

		public override void Update()
		{
			wave += Engine.DeltaTime * 4f;
			base.Update();
			AfterUpdate();
		}

		public void MoveHairBy(Vector2 amount)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				Nodes[i] += amount;
			}
		}

		public override void Render()
		{
			PlayerSprite sprite = Sprite;
			if (!sprite.HasHair)
			{
				return;
			}
			Vector2 origin = new Vector2(5f, 5f);
			Color color = Border * Alpha;
			if (DrawPlayerSpriteOutline)
			{
				Color color2 = sprite.Color;
				Vector2 position = sprite.Position;
				sprite.Color = color;
				sprite.Position = position + new Vector2(0f, -1f);
				sprite.Render();
				sprite.Position = position + new Vector2(0f, 1f);
				sprite.Render();
				sprite.Position = position + new Vector2(-1f, 0f);
				sprite.Render();
				sprite.Position = position + new Vector2(1f, 0f);
				sprite.Render();
				sprite.Color = color2;
				sprite.Position = position;
			}
			Nodes[0] = Nodes[0].Floor();
			if (color.A > 0)
			{
				for (int i = 0; i < sprite.HairCount; i++)
				{
					MTexture hairTexture = GetHairTexture(i);
					Vector2 hairScale = GetHairScale(i);
					hairTexture.Draw(Nodes[i] + new Vector2(-1f, 0f), origin, color, hairScale);
					hairTexture.Draw(Nodes[i] + new Vector2(1f, 0f), origin, color, hairScale);
					hairTexture.Draw(Nodes[i] + new Vector2(0f, -1f), origin, color, hairScale);
					hairTexture.Draw(Nodes[i] + new Vector2(0f, 1f), origin, color, hairScale);
				}
			}
			for (int num = sprite.HairCount - 1; num >= 0; num--)
			{
				GetHairTexture(num).Draw(Nodes[num], origin, GetHairColor(num), GetHairScale(num));
			}
		}

		private Vector2 GetHairScale(int index)
		{
			float num = (0.25f + (1f - (float)index / (float)Sprite.HairCount) * 0.75f) * Scale.Y;
			return new Vector2(((index == 0) ? ((float)Facing) : num) * Math.Abs(Sprite.Scale.X), num);
		}

		public MTexture GetHairTexture(int index)
		{
			if (index == 0)
			{
				return bangs[Sprite.HairFrame];
			}
			return GFX.Game["characters/player/hair00"];
		}

		public Vector2 PublicGetHairScale(int index)
		{
			return GetHairScale(index);
		}

		public Color GetHairColor(int index)
		{
			return Color * Alpha;
		}
	}
}
