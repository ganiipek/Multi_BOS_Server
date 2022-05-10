using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    internal class AccountGroup
    {
        public int Id { get; set; }
        public Account Account { get; set; }
        public Pair Pair { get; set; }
        public int Group { get; set; }
        public bool Master { get; set; }
        public double VolumeMin { get; set; }
        public double AllotedPercantage { get; set; }
    }
}
