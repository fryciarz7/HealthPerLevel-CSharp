namespace HealthPerLevel_cs.Interfaces
{
    public interface IIncreasePerLevel
    {
        public int thorax_health_per_level { get; set; }
        public int stomach_health_per_level { get; set; }
        public int head_health_per_level { get; set; }
        public int left_arm_per_level { get; set; }
        public int right_arm_per_level { get; set; }
        public int left_leg_per_level { get; set; }
        public int right_leg_per_level { get; set; }
    }
}