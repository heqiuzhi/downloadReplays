using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadDota2Replay
{
    public class Player
    {
        public UInt64 account_id { set; get; }
        public string name { set; get; }
        public string country_code { set; get; }
        public Int32  team_id { set; get; }
        public string team_name { set; get; }
        public string team_tag { set; get; }
    }
}
