using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect.Models
{
    class DayVolume
    {
        public DayVolume(string minute, int volumeBuy, int volumeSell)
        {
            Minute = minute;
            VolumeBuy = volumeBuy;
            VolumeSell = volumeSell;
        }

        public string Minute { get; set; }
        public int VolumeBuy { get; set; }
        public int VolumeSell { get; set; }
    }
}
