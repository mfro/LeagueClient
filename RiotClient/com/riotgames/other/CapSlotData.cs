using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFroehlich.Parsing.JSON;

namespace RiotClient.com.riotgames.other {
  public class CapSlotData : JSONSerializable {
    [JSONField("slotId")]
    public int SlotId { get; set; }

    [JSONField("summonerName")]
    public string SummonerName { get; set; }

    [JSONField("summonerIconId")]
    public int SummonerIconId { get; set; }

    [JSONField("championId")]
    public int ChampionId { get; set; }

    [JSONField("role")]
    public string Role { get; set; }

    [JSONField("advertisedRole")]
    public string AdvertisedRole { get; set; }

    [JSONField("position")]
    public string Position { get; set; }

    [JSONField("advertisedPosition")]
    public string AdvertisedPosition { get; set; }

    [JSONField("spell1Id")]
    public int Spell1Id { get; set; }

    [JSONField("spell2Id")]
    public int Spell2Id { get; set; }

    [JSONField("status")]
    public string Status { get; set; }
  }
}
