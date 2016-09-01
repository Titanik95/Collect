using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect.Models
{
    class DayVolume
    {
        public DayVolume(string minute, double volumeBuy, double volumeSell)
        {
            Minute = minute;
            VolumeBuy = volumeBuy;
            VolumeSell = volumeSell;
        }

        public string Minute { get; set; }
        public double VolumeBuy { get; set; }
        public double VolumeSell { get; set; }
    }
}
