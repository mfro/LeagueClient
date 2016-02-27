using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.matchmaking.QueueInfo")]
    public class QueueInfo
    {
        [SerializedName("waitTime")]
        public Double WaitTime { get; set; }

        [SerializedName("queueId")]
        public Int32 QueueId { get; set; }

        [SerializedName("queueLength")]
        public Int32 QueueLength { get; set; }
    }
}
