namespace HealthPerLevel_cs.Interfaces
{
    public interface IHealth
    {
        public int thorax_health { get; set; }
        public int stomach_health { get; set; }
        public int head_health { get; set; }
        public int left_arm_health { get; set; }
        public int right_arm_health { get; set; }
        public int left_leg_health { get; set; }
        public int right_leg_health { get; set; }
    }
}