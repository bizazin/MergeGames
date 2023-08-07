using JCMG.EntitasRedux;

namespace Ecs.BossFight.Components
{
	[BossFight, Unique]
	public class BossIdComponent : IComponent
	{
		public int Value;
	}
}