using Game.Models.BossFight;

namespace Ecs.BossFight.Utils
{
	public static class BossFightExtensions
	{
		public static BossFightEntity CreateBossFight(this BossFightContext context, BossInfoVo bossInfo,
			int fightDuration, int fightDamage)
		{
			var entity = context.CreateEntity();
			entity.AddBossId(bossInfo.id);
			entity.AddBossName(bossInfo.bossName);
			entity.AddBossHealth(bossInfo.hpBoss, bossInfo.hpBoss);
			entity.AddDamagePerSecond(fightDamage);
			entity.AddBossTimeSeconds(fightDuration);
			return entity;
		}

		public static void ReplaceBossCurrentHealth(this BossFightEntity entity, int currentHealth)
			=> entity.ReplaceBossHealth(currentHealth, entity.BossHealth.Max);
	}
}