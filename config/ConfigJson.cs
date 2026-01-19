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

    public class PMC : ICharacter<Base_Health_PMC, Increase_Per_Level_PMC, Increase_Per_Health_Skill_Level_PMC>
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public Base_Health_PMC base_health { get; set; }
        public Increase_Per_Level_PMC increase_per_level { get; set; }
        public int health_skill_levels_per_increment { get; set; }
        public bool level_health_skill_cap { get; set; }
        public int level_health_skill_cap_value { get; set; }
        public bool health_per_health_skill_level { get; set; }
        public Increase_Per_Health_Skill_Level_PMC increase_per_health_skill_level { get; set; }
    }

    public class Base_Health_PMC : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class Increase_Per_Level_PMC : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class Increase_Per_Health_Skill_Level_PMC : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class SCAV : ICharacter<Base_Health_SCAV, Increase_Per_Level_SCAV, Increase_Per_Health_Skill_Level_SCAV>
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public Base_Health_SCAV base_health { get; set; }
        public Increase_Per_Level_SCAV increase_per_level { get; set; }
        public int health_skill_levels_per_increment { get; set; }
        public bool level_health_skill_cap { get; set; }
        public int level_health_skill_cap_value { get; set; }
        public bool health_per_health_skill_level { get; set; }
        public Increase_Per_Health_Skill_Level_SCAV increase_per_health_skill_level { get; set; }
    }

    public class Base_Health_SCAV : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class Increase_Per_Level_SCAV : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }

    public class Increase_Per_Health_Skill_Level_SCAV : IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
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