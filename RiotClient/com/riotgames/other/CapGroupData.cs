using System;
using System.Collections.Generic;
using MFroehlich.Parsing.JSON;

namespace RiotClient.com.riotgames.other {
  public class CapGroupData : JSONSerializable {
    [JSONField("groupId")]
    public string GroupId { get; set; }

    [JSONField("minPremadeSize")]
    public int MinPremadeSize { get; set; }

    [JSONField("slotIds")]
    public int[] SlotIds { get; set; }

    [JSONField("slots")]
    public List<CapSlotData> Slots { get; set; }

    [JSONField("groupTtlSecs")]
    public int GroupTtlSeconds { get; set; }

    [JSONField("soloSpecRoles")]
    public List<string> AvailableRoles { get; set; }

    [JSONField("slotId")]
    public int SlotId { get; set; }

    [JSONField("initialChampionId")]
    public int InitialChampId { get; set; }

    [JSONField("initialRole")]
    public string InitialRole { get; set; }

    [JSONField("initialPosition")]
    public string InitialPosition { get; set; }

    [JSONField("playerInfoRetrieved")]
    public CapPlayerInfoData PlayerInfo { get; set; }
  }
}