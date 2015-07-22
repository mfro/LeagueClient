using System;
using RtmpSharp.IO;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.statistics.TimeTrackedStat")]
    public class TimeTrackedStat
    {
        [SerializedName("timestamp")]
        public DateTime Timestamp { get; set; }

        [SerializedName("type")]
        public String Type { get; set; }
    }
}
