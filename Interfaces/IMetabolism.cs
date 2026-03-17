using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthPerLevel_cs.Interfaces
{
    internal interface IMetabolism
    {
        float energy { get; set; }
        float hydration { get; set; }
    }
}
