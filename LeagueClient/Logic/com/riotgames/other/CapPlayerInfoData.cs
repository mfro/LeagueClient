using System;
using System.Collections.Generic;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.Logic.com.riotgames.other {
  public class CapPlayerInfoData {
    [JSONField("championIds")]
    public List<int> ChampionIds { get; set; }

    [JSONField("skinIds")]
    public List<int> SkinIds { get; set; }

    [JSONField("spellIds")]
    public List<int> SpellIds { get; set; }

    [JSONField("initialSpellIds")]
    public int[] InitialSpellIds { get; set; }

    [JSONField("adjustedRoles")]
    public List<string> AdjustedRoles { get; set; }

    [JSONField("demandInfo")]
    public Dictionary<string, object> DemandInfo { get; set; }

    [JSONField("lastPlayedChampionId")]
    public int LastChampId { get; set; }

    [JSONField("lastPlayedRole")]
    public string LastRole { get; set; }

    [JSONField("lastSelectedSkinIdByChampionIds")]
    public Dictionary<int, int> LastSkinsByChampion { get; set; }
  }
}