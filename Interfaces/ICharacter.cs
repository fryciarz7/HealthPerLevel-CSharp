namespace HealthPerLevel_cs.Interfaces
{
    public interface ICharacter<TBaseHealth, TIncreasePerLevel, TIncreasePerHealthSkillLevel, IMetabolism>// where TBaseHealth : IHealth
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public int health_skill_levels_per_increment { get; set; }
        public bool level_health_skill_cap { get; set; }
        public int level_health_skill_cap_value { get; set; }
        public TBaseHealth base_health { get; set; }
        public TIncreasePerLevel increase_per_level { get; set; }
        public bool health_per_health_skill_level { get; set; }
        public TIncreasePerHealthSkillLevel increase_per_health_skill_level { get; set; }
        public bool modify_energy_and_hydration { get; set; }
        public int metabolism_skill_levels_per_increment { get; set; }
        public bool metabolism_skill_cap { get; set; }
        public int metabolism_skill_cap_value { get; set; }
        public IMetabolism metabolism { get; set; }
        public IMetabolism metabolism_per_skill { get; set; }
    }
}