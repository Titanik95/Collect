using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Collect.Enums;

namespace Collect.Models
{
    class DayTrade
    {
        public DayTrade(DateTime time, decimal price, double volume, Direction direction)
        {
            Time = time;
            Price = price;
            Volume = volume;
            Direction = direction;
        }

        public DateTime Time { get; set; }
        public decimal Price { get; set; }
        public double Volume { get; set; }
        public Direction Direction { get; set; }
    }
}
