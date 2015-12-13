using MFroehlich.Parsing.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic.com.riotgames.JSON {
  [JSONSerializable]
  public class ChampionMasteryDTO {
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
