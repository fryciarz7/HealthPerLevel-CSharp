namespace HealthPerLevel_cs.Interfaces
{
    public interface IHealth
    {
        public float thorax_health { get; set; }
        public float stomach_health { get; set; }
        public float head_health { get; set; }
        public float left_arm_health { get; set; }
        public float right_arm_health { get; set; }
        public float left_leg_health { get; set; }
        public float right_leg_health { get; set; }
    }
}