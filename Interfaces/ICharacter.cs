namespace HealthPerLevel_cs.Interfaces
{
    public interface ICharacter
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
    }
}