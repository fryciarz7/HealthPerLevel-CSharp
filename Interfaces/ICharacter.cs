namespace HealthPerLevel_cs.Interfaces
{
    public interface ICharacter<TBaseHealth, TIncreasePerLevel>
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public TBaseHealth base_health { get; set; }
        public TIncreasePerLevel increase_per_level { get; set; }
    }
}