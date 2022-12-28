using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace vitmod
{
    [CustomEntity("vitellary/cassetteflags")]
    public class CassetteFlagController : Entity
    {
        public CassetteFlagController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            blueFlag = data.Attr("blueFlag", "cas_blue");
            pinkFlag = data.Attr("pinkFlag", "cas_rose");
            yellowFlag = data.Attr("yellowFlag", "cas_brightsun");
            greenFlag = data.Attr("greenFlag", "cas_malachite");
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            CassetteBlockManager cassette = scene.Tracker.GetEntity<CassetteBlockManager>();
            if (cassette != null)
            {
                cassetteData = new DynData<CassetteBlockManager>(cassette);
            }
            else
            {
                RemoveSelf();
            }
        }

        public override void Update()
        {
            base.Update();
            if (cassetteData != null)
            {
                int index = cassetteData.Get<int>("currentIndex");
                Session session = SceneAs<Level>().Session;
                switch (index)
                {
                    case 0:
                        session.SetFlag(blueFlag, true);
                        session.SetFlag(pinkFlag, false);
                        session.SetFlag(yellowFlag, false);
                        session.SetFlag(greenFlag, false);
                        break;
                    case 1:
                        session.SetFlag(blueFlag, false);
                        session.SetFlag(pinkFlag, true);
                        session.SetFlag(yellowFlag, false);
                        session.SetFlag(greenFlag, false);
                        break;
                    case 2:
                        session.SetFlag(blueFlag, false);
                        session.SetFlag(pinkFlag, false);
                        session.SetFlag(yellowFlag, true);
                        session.SetFlag(greenFlag, false);
                        break;
                    case 3:
                        session.SetFlag(blueFlag, false);
                        session.SetFlag(pinkFlag, false);
                        session.SetFlag(yellowFlag, false);
                        session.SetFlag(greenFlag, true);
                        break;
                }
            }
        }

        private DynData<CassetteBlockManager> cassetteData;

        private string blueFlag;
        private string pinkFlag;
        private string yellowFlag;
        private string greenFlag;
    }
}
