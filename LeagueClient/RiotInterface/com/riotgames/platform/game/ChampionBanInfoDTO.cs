using System;
using RtmpSharp.IO;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.game.ChampionBanInfoDTO")]
    public class ChampionBanInfoDTO
    {
        [SerializedName("enemyOwned")]
        public Boolean EnemyOwned { get; set; }

        [SerializedName("championId")]
        public Int32 ChampionId { get; set; }

        [SerializedName("owned")]
        public Boolean Owned { get; set; }
    }
}
