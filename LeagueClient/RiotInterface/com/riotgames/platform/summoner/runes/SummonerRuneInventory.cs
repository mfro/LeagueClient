using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.summoner.runes.SummonerRuneInventory")]
    public class SummonerRuneInventory
    {
        [SerializedName("summonerRunesJson")]
        public object SummonerRunesJson { get; set; }

        [SerializedName("dateString")]
        public String DateString { get; set; }

        [SerializedName("summonerRunes")]
        public List<SummonerRune> SummonerRunes { get; set; }

        [SerializedName("summonerId")]
        public Double SummonerId { get; set; }
    }
}
