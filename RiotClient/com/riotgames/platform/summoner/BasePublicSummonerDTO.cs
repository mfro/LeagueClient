using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.summoner.BasePublicSummonerDTO")]
    public class BasePublicSummonerDTO
    {
        [SerializedName("seasonTwoTier")]
        public String SeasonTwoTier { get; set; }

        [SerializedName("publicName")]
        public String InternalName { get; set; }

        [SerializedName("seasonOneTier")]
        public String SeasonOneTier { get; set; }

        [SerializedName("acctId")]
        public Int64 AccountId { get; set; }

        [SerializedName("name")]
        public String Name { get; set; }

        [SerializedName("sumId")]
        public Int64 SummonerId { get; set; }

        [SerializedName("profileIconId")]
        public Int32 ProfileIconId { get; set; }
    }
}
