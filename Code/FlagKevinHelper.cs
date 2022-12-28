using System.Collections.Generic;
using System.Linq;
using Celeste;
using Monocle;

namespace vitmod
{
    [Tracked]
    public class FlagKevinHelper : Entity
    {
        public FlagKevinHelper() : base() { }
        public IEnumerable<Entity> LavaEntities { get; private set; } = Enumerable.Empty<Entity>();

        public override void Update()
        {
            LavaEntities = Scene.Where((e) => e.Get<LavaRect>() != null);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (Scene.Any((e) => e != this && e is FlagKevinHelper)) { RemoveSelf(); }
        }
    }
}