using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi_BOS_Server.Trade
{
    internal class Broker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlatformId { get; set; }

        public Broker()
        {
            Id = -1;
            Name = "";
            PlatformId = -1;
        }

        public override string ToString()
        {
            return String.Format("Id: {0}, Name: {1}, Platform: {2}",
                Id.ToString(),
                Name,
                PlatformId.ToString()
            );
        }
    }
}
