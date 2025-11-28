namespace HealthPerLevel_cs.Interfaces
{
    public interface IBaseHealth
    {
        public int thorax_base_health { get; set; }
        public int stomach_base_health { get; set; }
        public int head_base_health { get; set; }
        public int left_arm_base_health { get; set; }
        public int right_arm_base_health { get; set; }
        public int left_leg_base_health { get; set; }
        public int right_leg_base_health { get; set; }
    }
}