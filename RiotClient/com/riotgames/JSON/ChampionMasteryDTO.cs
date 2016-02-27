using MFroehlich.Parsing.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.com.riotgames.JSON {
  public class ChampionMasteryDTO : JSONSerializable {
    [JSONField("championId")]
    public int ChampionId { get; set; }

    [JSONField("playerId")]
    public double SummonerId { get; set; }

    [JSONField("championLevel")]
    public int ChampionLevel { get; set; }

    [JSONField("championPoints")]
    public int ChampionPoints { get; set; }

    [JSONField("lastPlayTime")]
    public int LastPlayTime { get; set; }

    [JSONField("championPointsSinceLastLevel")]
    public int ChampionPointsSinceLastLevel { get; set; }

    [JSONField("championPointsUntilNextLevel")]
    public int ChampionPointsUntilNextLevel { get; set; }
  }
}
