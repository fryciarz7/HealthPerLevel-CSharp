using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthPerLevel_cs.config
{
    public class ConfigJson
    {
        public bool enabled { get; set; }
        public PMC PMC { get; set; }
    }

    public class PMC
    {
        public int levels_per_increment { get; set; }
        public bool level_cap { get; set; }
        public int level_cap_value { get; set; }
        public Base_Health base_health { get; set; }
        public Increase_Per_Level increase_per_level { get; set; }
    }

    public class Base_Health
    {
        public int thorax_base_health { get; set; }
        public int stomach_base_health { get; set; }
        public int head_base_health { get; set; }
        public int left_arm_base_health { get; set; }
        public int right_arm_base_health { get; set; }
        public int left_leg_base_health { get; set; }
        public int right_leg_base_health { get; set; }
    }

    public class Increase_Per_Level
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