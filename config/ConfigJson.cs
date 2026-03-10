using HealthPerLevel_cs.Interfaces;

namespace HealthPerLevel_cs.config
{
    public class ConfigJson
    {
        public bool enabled { get; set; }
        public bool restoreDefaults { get; set; }
        public bool debug { get; set; }
        public PMC PMC { get; set; }
        public SCAV SCAV { get; set; }
        public AI AI { get; set; }
    }

    // Shared types to reduce duplication between PMC and SCAV
    public class BodyHealth : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class Metabolism : IMetabolism
    {
        public float energy { get; set; }
        public float hydration { get; set; }
    }

    public class PMC : ICharacter<BodyHealth, BodyHealth, BodyHealth, Metabolism>
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public BodyHealth base_health { get; set; }
        public BodyHealth increase_per_level { get; set; }
        public int health_skill_levels_per_increment { get; set; }
        public bool level_health_skill_cap { get; set; }
        public int level_health_skill_cap_value { get; set; }
        public bool health_per_health_skill_level { get; set; }
        public BodyHealth increase_per_health_skill_level { get; set; }
        public bool modify_energy_and_hydration { get; set; }
        public int metabolism_skill_levels_per_increment { get; set; }
        public bool metabolism_skill_cap { get; set; }
        public int metabolism_skill_cap_value { get; set; }
        public Metabolism metabolism { get; set; }
        public Metabolism metabolism_per_skill { get; set; }
    }

    public class SCAV : ICharacter<BodyHealth, BodyHealth, BodyHealth, Metabolism>
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public BodyHealth base_health { get; set; }
        public BodyHealth increase_per_level { get; set; }
        public int health_skill_levels_per_increment { get; set; }
        public bool level_health_skill_cap { get; set; }
        public int level_health_skill_cap_value { get; set; }
        public bool health_per_health_skill_level { get; set; }
        public BodyHealth increase_per_health_skill_level { get; set; }
        public bool modify_energy_and_hydration { get; set; }
        public int metabolism_skill_levels_per_increment { get; set; }
        public bool metabolism_skill_cap { get; set; }
        public int metabolism_skill_cap_value { get; set; }
        public Metabolism metabolism { get; set; }
        public Metabolism metabolism_per_skill { get; set; }
    }

    public class AI
    {
        public bool enabled { get; set; }
        public bool pmc_bot_health { get; set; }
        public bool scav_bot_health { get; set; }
        public bool boss_bot_health { get; set; }
        public bool follower_bot_health { get; set; }
        public bool special_bot_health { get; set; }
        public bool raider_bot_health { get; set; }
        public bool cultist_bot_health { get; set; }
        public bool event_boss_health { get; set; }
    }
}
