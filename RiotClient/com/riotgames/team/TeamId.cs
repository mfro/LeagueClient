using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Team
{
    [Serializable]
    [SerializedName("com.riotgames.team.TeamId")]
    public class TeamId
    {
        [SerializedName("fullId")]
        public string FullId { get; set; }
    }
}
