using System;

namespace Neko.EFT.Manager.X.Classes
{
    public class SessionManager
    {
        private static string sessionId;

        public static string SessionId
        {
            get { return sessionId; }
            set { sessionId = value; }
        }
    }
}
