using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.matchmaking.QueueDodger")]
    public class QueueDodger
    {
        [SerializedName("reasonFailed")]
        public string ReasonFailed { get; set; }

        [SerializedName("summoner")]
        public Summoner Summoner { get; set; }

        [SerializedName("dodgePenaltyRemainingTime")]
        public Int32 PenaltyRemainingTime { get; set; }
    }
}
