using System.Runtime.Serialization;

namespace Neko.EFT.Manager.X.Classes.CoreModules
{
    [DataContract]
    public class FikaPlayerPresence // 改为 public
    {
        [DataMember(Name = "nickname")]
        public string Nickname { get; set; }

        [DataMember(Name = "level")]
        public int Level { get; set; }

        [DataMember(Name = "activity")]
        public int Activity { get; set; }

        [DataMember(Name = "activityStartedTimestamp")]
        public long ActivityStartedTimestamp { get; set; }

        [DataMember(Name = "raidInformation")]
        public RaidInformation? RaidInformation { get; set; }

        public FikaPlayerPresence() { }

        public FikaPlayerPresence(string nickname, int level, int activity, long activityStartedTimestamp, RaidInformation? raidInformation)
        {
            Nickname = nickname;
            Level = level;
            Activity = activity;
            ActivityStartedTimestamp = activityStartedTimestamp;
            RaidInformation = raidInformation;
        }
    }

    [DataContract]
    public class RaidInformation
    {
        [DataMember(Name = "location")]
        public string Location { get; set; }

        [DataMember(Name = "side")]
        public string Side { get; set; }

        [DataMember(Name = "time")]
        public string Time { get; set; }

        public RaidInformation() { }

        public RaidInformation(string location, string side, string time)
        {
            Location = location;
            Side = side;
            Time = time;
        }
    }
}
