using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;

namespace vitmod {
    [Tracked()]
    [CustomEntity("vitellary/templegateall")]
    public class TempleGateAllSwitches : TempleGate {
        public TempleGateAllSwitches(EntityData data, Vector2 offset) : base(data.Position + offset, 48, Types.NearestSwitch, data.Attr("sprite", "default"), data.Level.Name) {
            ClaimedByASwitch = true;
        }

        private static void HookOnDashCollide(DashSwitch dashSwitch) {
            DashCollision orig_OnDashCollide = dashSwitch.OnDashCollide;
            
            dashSwitch.OnDashCollide = (player, direction) => {
                DashCollisionResults result = orig_OnDashCollide(player, direction);
                if (!dashSwitch.pressed || dashSwitch.Scene.Entities.Any(entity => entity is DashSwitch { pressed: false }))
                    return result;
                
                foreach (TempleGateAllSwitches gate in dashSwitch.Scene.Tracker.GetEntities<TempleGateAllSwitches>().Cast<TempleGateAllSwitches>())
                    gate.Open();

                return result;
            };
        }
        
        #region Hooks

        internal static void Load() {
            IL.Monocle.EntityList.UpdateLists += EntityList_UpdateLists;
        }

        internal static void Unload() {
            IL.Monocle.EntityList.UpdateLists -= EntityList_UpdateLists;
        }

        private static void EntityList_UpdateLists(ILContext il) {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNextBestFit(MoveType.After,
                instr => instr.MatchLdloc(5),
                instr => instr.MatchLdarg0(),
                instr => instr.MatchCallvirt<EntityList>("get_Scene"),
                instr => instr.MatchCallvirt<Entity>("Awake")))
                return;
        
            cursor.EmitLdloc(5);
            cursor.EmitDelegate(ProcessDashSwitch);

            return;

            static void ProcessDashSwitch(Entity entity) {
                if (entity is DashSwitch dashSwitch)
                    HookOnDashCollide(dashSwitch);
            }
        }
        
        #endregion
    }
}
