using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace FlushelineCollab.Entities
{
    [CustomEntity("FlushelineCollab/CassetteFlagController")]
    public class CassetteFlagController : Entity
    {
        public CassetteFlagController(EntityData data, Vector2 offset) : base(data.Position + offset) { }

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
                        session.SetFlag("cas_blue", true);
                        session.SetFlag("cas_rose", false);
                        session.SetFlag("cas_brightsun", false);
                        session.SetFlag("cas_malachite", false);
                        break;
                    case 1:
                        session.SetFlag("cas_blue", false);
                        session.SetFlag("cas_rose", true);
                        session.SetFlag("cas_brightsun", false);
                        session.SetFlag("cas_malachite", false);
                        break;
                    case 2:
                        session.SetFlag("cas_blue", false);
                        session.SetFlag("cas_rose", false);
                        session.SetFlag("cas_brightsun", true);
                        session.SetFlag("cas_malachite", false);
                        break;
                    case 3:
                        session.SetFlag("cas_blue", false);
                        session.SetFlag("cas_rose", false);
                        session.SetFlag("cas_brightsun", false);
                        session.SetFlag("cas_malachite", true);
                        break;
                }
            }
        }

        private DynData<CassetteBlockManager> cassetteData;
    }
}
