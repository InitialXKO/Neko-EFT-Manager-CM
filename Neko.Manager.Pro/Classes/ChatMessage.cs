using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class ChatMessage
    {
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
