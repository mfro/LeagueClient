using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.summoner.SummonerDefaultSpells")]
    public class SummonerDefaultSpells
    {
        [SerializedName("summonerDefaultSpellsJson")]
        public object SummonerDefaultSpellsJson { get; set; }

        [SerializedName("summonerDefaultSpellMap")]
        public Dictionary<string, SummonerGameModeSpells>  SummonerDefaultSpellMap { get; set; }

        [SerializedName("summonerId")]
        public Double SummonerId { get; set; }
    }
}
