using System;
using System.Collections.Generic;
using MFroehlich.Parsing.JSON;

namespace RiotClient.com.riotgames.other {
  public class TBDGroupData : JSONSerializable {
    [JSONField("counter")]
    public int Counter { get; set; }

    [JSONField("phaseName")]
    public string PhaseName { get; set; }

    [JSONField("premadeState")]
    public TBDPhase Phase { get; set; }

    [JSONField("championSelectState")]
    public TBDChampSelectState ChampSelectState { get; set; }
  }

  public class TBDChampSelectState : JSONSerializable {
    [JSONField("teamId")]
    public Guid TeamId { get; set; }

    [JSONField("teamChatRoomId")]
    public Guid TeamChatRoomId { get; set; }

    [JSONField("subphase")]
    public string Subphase { get; set; }

    [JSONField("actionSetList")]
    public List<TBDChampSelectAction> Actions { get; set; }

    [JSONField("currentActionSetIndex")]
    public int CurrentActionIndex { get; set; }

    [JSONField("cells")]
    public TBDCells Cells { get; set; }

    [JSONField("localPlayerCell")]
    public int MyCellId { get; set; }

    [JSONField("bans")]
    public TBDBans Bans { get; set; }

    [JSONField("currentTotalTimeMillis")]
    public int CurrentTotalMillis { get; set; }

    [JSONField("currentTimeRemainingMillis")]
    public int CurrentRemainingMillis { get; set; }
  }

  public class TBDBans : JSONSerializable {
    [JSONField("alliedBans")]
    public List<int> AlliedBans { get; set; }

    [JSONField("enemyBans")]
    public List<int> EnemyBans { get; set; }
  }

  public class TBDCells : JSONSerializable {
    [JSONField("alliedTeam")]
    public TBDCell[] AlliedTeam { get; set; }

    [JSONField("enemyTeam")]
    public TBDCell[] EnemyTeam { get; set; }
  }

  public class TBDCell : JSONSerializable {
    [JSONField("teamId")]
    public int TeamId { get; set; }

    [JSONField("cellId")]
    public int CellId { get; set; }

    [JSONField("summonerName")]
    public string Name { get; set; }

    [JSONField("championPickIntent")]
    public int ChampionPickIntent { get; set; }

    [JSONField("championId")]
    public int ChampionId { get; set; }

    [JSONField("assignedPosition")]
    public string AssignedPosition { get; set; }

    [JSONField("spell1Id")]
    public int Spell1Id { get; set; }

    [JSONField("spell2Id")]
    public int Spell2Id { get; set; }
  }

  public class TBDChampSelectAction : JSONSerializable {
    [JSONField("actionId")]
    public int ActionId { get; set; }

    [JSONField("actorCellId")]
    public int ActorCellId { get; set; }

    [JSONField("type")]
    public string Type { get; set; }

    [JSONField("championId")]
    public int ChampionId { get; set; }

    [JSONField("completed")]
    public bool Completed { get; set; }
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