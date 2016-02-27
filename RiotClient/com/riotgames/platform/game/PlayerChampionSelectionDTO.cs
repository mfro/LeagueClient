using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.game.PlayerChampionSelectionDTO")]
    public class PlayerChampionSelectionDTO
    {
        [SerializedName("summonerInternalName")]
        public String SummonerInternalName { get; set; }

        [SerializedName("spell2Id")]
        public Int32 Spell2Id { get; set; }

        [SerializedName("selectedSkinIndex")]
        public Int32 SelectedSkinIndex { get; set; }

        [SerializedName("championId")]
        public Int32 ChampionId { get; set; }

        [SerializedName("spell1Id")]
        public Int32 Spell1Id { get; set; }
    }
}
