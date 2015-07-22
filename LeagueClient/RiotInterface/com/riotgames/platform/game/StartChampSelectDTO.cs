using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.game.StartChampSelectDTO")]
    public class StartChampSelectDTO
    {
        [SerializedName("invalidPlayers")]
        public List<object> InvalidPlayers { get; set; }
    }
}
