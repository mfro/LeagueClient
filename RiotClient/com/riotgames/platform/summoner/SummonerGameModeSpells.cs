using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.summoner.SummonerGameModeSpells")]
  public class SummonerGameModeSpells {
    [SerializedName("dataVersion")]
    public Int32 DataVersion { get; set; }

    [SerializedName("spell1Id")]
    public Int32 Spell1Id { get; set; }

    [SerializedName("spell2Id")]
    public Int32 Spell2Id { get; set; }
  }
}
