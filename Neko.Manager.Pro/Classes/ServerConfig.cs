using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class ServerConfig
    {
        public string name { get; set; }
        public string serverAddress { get; set; }
        public string newPort { get; set; }

        public static implicit operator string(ServerConfig v)
        {
            throw new NotImplementedException();
        }
    }
}
