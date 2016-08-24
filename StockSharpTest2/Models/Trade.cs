using SmartCOM3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect.Models
{
    class Trade
    {
        public string Symbol { get; set; }

        public DateTime Time { get; set; }

        public double Price { get; set; }

        public double Volume { get; set; }

        public string TradeId { get; set; }

        public StOrder_Action Direction { get; set; }
    }
}
