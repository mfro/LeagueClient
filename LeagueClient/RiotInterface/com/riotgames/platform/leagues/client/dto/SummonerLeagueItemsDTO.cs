using System;
using RtmpSharp.IO;
using System.Collections.Generic;
using LeagueClient.RiotInterface.Riot.Leagues;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.leagues.client.dto.SummonerLeagueItemsDTO")]
    public class SummonerLeagueItemsDTO
    {
        [SerializedName("summonerLeagues")]
        public List<LeagueItemDTO> SummonerLeagues { get; set; }
    }
}
