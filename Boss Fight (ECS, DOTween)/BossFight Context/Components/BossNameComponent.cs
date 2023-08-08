using JCMG.EntitasRedux;

namespace Ecs.BossFight.Components
{
	[BossFight, Unique]
	public class BossNameComponent : IComponent
	{
		public string Value;
	}
}