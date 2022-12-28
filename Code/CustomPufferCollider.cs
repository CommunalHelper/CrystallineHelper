using Monocle;
using System;

namespace vitmod
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
				Collider collider = Entity.Collider;
				if (Collider != null)
				{
					Entity.Collider = Collider;
				}
				if (puffer.CollideCheck(Entity))
				{
					OnCollide(puffer);
				}
				Entity.Collider = collider;
			}
		}
	}
}
