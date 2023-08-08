using JCMG.EntitasRedux;

namespace Ecs.BossFight.Components
{
	[BossFight, Unique]
	public class BossTimeSecondsComponent : IComponent
	{
		public int Value;
	}
}