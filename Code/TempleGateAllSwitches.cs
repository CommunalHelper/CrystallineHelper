using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace vitmod
{
    [Tracked()]
    [CustomEntity("vitellary/templegateall")]
    public class TempleGateAllSwitches : TempleGate
    {
        public TempleGateAllSwitches(EntityData data, Vector2 offset) : base(data.Position + offset, 48, Types.NearestSwitch, data.Attr("sprite", "default"), data.Level.Name)
        {
            ClaimedByASwitch = true;
        }

        public static void Load()
        {
            On.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
        }

        public static void Unload()
        {
            On.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
        }

        private static DashCollisionResults DashSwitch_OnDashed(On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction)
        {
            DashCollisionResults result = orig(self, player, direction);
            bool finalswitch = true;
            DynData<DashSwitch> switchData = new DynData<DashSwitch>(self);
            if (switchData.Get<bool>("pressed"))
            {
                foreach (Solid solid in self.SceneAs<Level>().Tracker.GetEntities<Solid>())
                {
                    if (solid is DashSwitch)
                    {
                        DynData<DashSwitch> otherSwitchData = new DynData<DashSwitch>(solid as DashSwitch);
                        if (!otherSwitchData.Get<bool>("pressed"))
                        {
                            finalswitch = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                finalswitch = false;
            }
            if (finalswitch)
            {
                foreach (TempleGateAllSwitches gate in self.SceneAs<Level>().Tracker.GetEntities<TempleGateAllSwitches>())
                {
                    gate.Open();
                }
            }
            return result;
        }
    }
}
