﻿using System;
using RtmpSharp.IO;

namespace LeagueClient.RiotInterface.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.catalog.icon.Icon")]
    public class Icon
    {
        [SerializedName("purchaseDate")]
        public DateTime PurchaseDate { get; set; }

        [SerializedName("iconId")]
        public Double IconId { get; set; }

        [SerializedName("summonerId")]
        public Double SummonerId { get; set; }
    }
}
