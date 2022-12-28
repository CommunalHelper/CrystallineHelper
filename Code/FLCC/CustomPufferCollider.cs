using Monocle;
using System;

namespace FlushelineCollab.Entities
{
	[Tracked(false)]
	public class CustomPufferCollider : Component
	{
		public Action<CustomPuffer> OnCollide;

		public Collider Collider;

		public CustomPufferCollider(Action<CustomPuffer> onCollide, Collider collider = null)
			: base(active: false, visible: false)
		{
			OnCollide = onCollide;
			Collider = null;
		}

		public void Check(CustomPuffer puffer)
		{
			if (OnCollide != null)
			{
				Collider collider = base.Entity.Collider;
				if (Collider != null)
				{
					base.Entity.Collider = Collider;
				}
				if (puffer.CollideCheck(base.Entity))
				{
					OnCollide(puffer);
				}
				base.Entity.Collider = collider;
			}
		}
	}
}
