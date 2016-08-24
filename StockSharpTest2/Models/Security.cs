using System;

namespace Collect.Models
{
    [Serializable]
    public class Security : IComparable<Security>
    {
        public Security(string symbol, string shortName, string secExchName, string type)
        {
            Code = symbol;
            ShortName = shortName;
            SecExchName = secExchName;
            Type = type;
        }

        public Security(string symbol, string shortName, string longName,
            string type, int decimals, int lotSize, double punkt, double step,
            string secExtId, string secExchName, DateTime expiryDate, double daysBeforeExpiry)
        {
            Code = symbol;
            ShortName = shortName;
            LongName = longName;
            Type = type;
            Decimals = decimals;
            LotSize = lotSize;
            Punkt = punkt;
            Step = step;
            SecExtId = secExtId;
            SecExchName = secExchName;
            ExpiryDate = expiryDate;
            DaysBeforeExpiry = daysBeforeExpiry;
        }

        public string Code { get; set; } 

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public string Type { get; set; }

        public int Decimals { get; set; }

        public int LotSize { get; set; }

        public double Punkt { get; set; }

        public double Step { get; set; }

        public string SecExtId { get; set; }

        public string SecExchName { get; set; }

        public DateTime ExpiryDate { get; set; }

        public double DaysBeforeExpiry { get; set; }

        public double Strike { get; set; }

        public int CompareTo(Security other)
        {
            return string.Compare(Code, other.Code);
        }

        public override string ToString()
        {
            return ShortName;
        }
    }
}
