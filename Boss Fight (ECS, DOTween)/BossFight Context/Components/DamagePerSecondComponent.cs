using JCMG.EntitasRedux;

namespace Ecs.BossFight.Components
{
	[BossFight, Unique, Event(EventTarget.Self)]
	public class DamagePerSecondComponent : IComponent
	{
		public int Value;
	}
}