﻿using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.summoner.icon.SummonerIconInventoryDTO")]
    public class SummonerIconInventoryDTO
    {
        [SerializedName("summonerId")]
        public Double SummonerId { get; set; }

        [SerializedName("summonerIcons")]
        public List<Icon> SummonerIcons { get; set; }
    }
}
