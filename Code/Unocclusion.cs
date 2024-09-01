using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace vitmod {
    [CustomEntity("vitellary/unocclusion")]
    [Tracked]
    public class Unocclusion : Entity {
        public Unocclusion() : base() { }

        public static void Load() {
            On.Celeste.LightingRenderer.BeforeRender += LightingRenderer_Render;
        }
        public static void Unload() {
            On.Celeste.LightingRenderer.BeforeRender -= LightingRenderer_Render;
        }

        public static void LightingRenderer_Render(On.Celeste.LightingRenderer.orig_BeforeRender orig, LightingRenderer self, Scene scene) {
            if (scene.Tracker.GetEntity<Unocclusion>() != null) {
                BlendFunction alphaStart = LightingRenderer.OccludeBlendState.AlphaBlendFunction;
                BlendFunction colorStart = LightingRenderer.OccludeBlendState.ColorBlendFunction;
                LightingRenderer.OccludeBlendState.AlphaBlendFunction = BlendFunction.Add;
                LightingRenderer.OccludeBlendState.ColorBlendFunction = BlendFunction.Add;
                orig(self, scene);
                LightingRenderer.OccludeBlendState.AlphaBlendFunction = alphaStart;
                LightingRenderer.OccludeBlendState.ColorBlendFunction = colorStart;
            } else {
                orig(self, scene);
            }
        }
    }
}
