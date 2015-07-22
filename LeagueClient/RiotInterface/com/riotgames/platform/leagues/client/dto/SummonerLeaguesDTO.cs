using System;
using RtmpSharp.IO;
using System.Collections.Generic;
using LeagueClient.RiotInterface.Riot.Leagues;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.leagues.client.dto.SummonerLeaguesDTO")]
    public class SummonerLeaguesDTO
    {
        [SerializedName("summonerLeagues")]
        public List<LeagueListDTO> SummonerLeagues { get; set; }
    }
}
