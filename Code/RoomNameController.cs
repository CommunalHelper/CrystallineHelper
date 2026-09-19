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
using static FrostHelper.CustomZipMover;

namespace Celeste.Mod.Code.Entities
{
    [CustomEntity("vitellary/roomname")]
    [Tracked]
    public class RoomNameController : Entity
    {
        public string Name;
        private string bgColor;
        private string textColor;
        private string lineColor;
        private float lineAmt;
        private float timer;
        private float scale;
        private float yOffset;

        public RoomNameController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Name = data.Attr("roomName");
            bgColor = data.Attr("backgroundColor", "000000FF");
            textColor = data.Attr("textColor", "FFFFFFFF");
            lineColor = data.Attr("outlineColor", "000000FF");
            lineAmt = data.Float("outlineThickness", 0f);
            scale = data.Float("scale", 1f);
            yOffset = data.Float("offset", 0f);

            timer = data.Float("disappearTimer", -1f);
        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            var display = RoomNameDisplay.GetDisplay(scene);
            display.SetName(Name);
            display.SetColor(textColor, bgColor, lineColor, lineAmt);
            display.SetTimer(Math.Max(timer, 0f));
            display.nextOffset = yOffset;
            display.prevScale = display.scale;
            display.nextScale = scale;
        }
    }
}
