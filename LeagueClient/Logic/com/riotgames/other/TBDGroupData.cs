using System;
using System.Collections.Generic;
using MFroehlich.Parsing.JSON;

namespace LeagueClient.Logic.com.riotgames.other {
  public class TBDGroupData : JSONSerializable {
    [JSONField("counter")]
    public int Counter { get; set; }

    [JSONField("phaseName")]
    public string PhaseName { get; set; }

    [JSONField("premadeState")]
    public TBDPhase Phase { get; set; }
  }

  public class TBDPhase : JSONSerializable {
    [JSONField("timer")]
    public int Timer { get; set; }

    [JSONField("draftPremadeId")]
    public Guid PremadeID { get; set; }

    [JSONField("premadeChatRoomId")]
    public string ChatRoomID { get; set; }

    [JSONField("captainSlotId")]
    public int CaptainSlot { get; set; }

    [JSONField("readyToMatchmake")]
    public bool ReadyToQueue { get; set; }

    [JSONField("draftSlots")]
    public List<TBDSlotData> Slots { get; set; }

    [JSONField("playableDraftPositions")]
    public List<string> PlayablePositions { get; set; }

    [JSONField("localPlayerSlotId")]
    public int MySlot { get; set; }
  }

  public class TBDSlotData : JSONSerializable {
    [JSONField("slotId")]
    public int SlotId { get; set; }

    [JSONField("summonerName")]
    public string SummonerName { get; set; }

    [JSONField("draftPositionPreferences")]
    public List<string> Positions { get; set; }
  }
}