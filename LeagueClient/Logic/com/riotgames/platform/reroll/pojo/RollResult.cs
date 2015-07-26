using System;
using RtmpSharp.IO;

namespace LeagueClient.Logic.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.reroll.pojo.RollResult")]
    public class RollResult
    {
        [SerializedName("championId")]
        public Int32 ChampionId { get; set; }

        [SerializedName("pointSummary")]
        public PointSummary PointSummary { get; set; }
    }
}
