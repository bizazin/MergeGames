using JCMG.EntitasRedux;

namespace Ecs.BossFight.Components
{
	[BossFight, Unique, Event(EventTarget.Self)]
	public class BossHealthComponent : IComponent
	{
		public int Current;
		public int Max;
	}
}