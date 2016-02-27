using System.Collections.Generic;

namespace RiotClient {
	public class TBDRole {
    public static readonly Dictionary<string, TBDRole> Values = new Dictionary<string, TBDRole>();

    public static readonly TBDRole
      UNSELECTED = new TBDRole("UNSELECTED", "?"),
      UTILITY = new TBDRole("UTILITY", "Support"),
      JUNGLE = new TBDRole("JUNGLE", "Jungle"),
      MIDDLE = new TBDRole("MIDDLE", "Middle"),
      BOTTOM = new TBDRole("BOTTOM", "Bottom"),
      TOP = new TBDRole("TOP", "Top"),
      FILL = new TBDRole("FILL", "Fill");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private TBDRole(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class Position {
    public static readonly Dictionary<string, Position> Values = new Dictionary<string, Position>();

    public static readonly Position
      TOP = new Position("TOP", "Top Lane"),
      JUNGLE = new Position("JUNGLE", "Jungle"),
      MIDDLE = new Position("MIDDLE", "Middle Lane"),
      BOTTOM = new Position("BOTTOM", "Bottom Lane"),
      UNSELECTED = new Position("UNSELECTED", "Unselected");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private Position(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class Role {
    public static readonly Dictionary<string, Role> Values = new Dictionary<string, Role>();

    public static readonly Role
      SUPPORT = new Role("SUPPORT", "Support"),
      MAGE = new Role("MAGE", "Mage"),
      MARKSMAN = new Role("MARKSMAN", "Marksman"),
      ASSASSIN = new Role("ASSASSIN", "Assassin"),
      FIGHTER = new Role("FIGHTER", "Fighter"),
      TANK = new Role("TANK", "Tank"),
      ANY = new Role("ANY", "Any"),
      UNSELECTED = new Role("UNSELECTED", "Unselected");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private Role(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class QueueType {
    public static readonly Dictionary<string, QueueType> Values = new Dictionary<string, QueueType>();

    public static readonly QueueType
      CUSTOM = new QueueType("CUSTOM", "Custom"),
      NONE = new QueueType("NONE", "Custom"),
      NORMAL = new QueueType("NORMAL", "Normal 5v5"),
      NORMAL_3x3 = new QueueType("NORMAL_3x3", "Normal 3v3"),
      ODIN_UNRANKED = new QueueType("ODIN_UNRANKED", "Dominion"),
      ARAM_UNRANKED_5x5 = new QueueType("ARAM_UNRANKED_5x5", "ARAM 5v5"),
      BOT = new QueueType("BOT", "Co-op vs AI 5v5"),
      BOT_3x3 = new QueueType("BOT_3x3", "Co-op vs AI 3v3"),
      RANKED_SOLO_5x5 = new QueueType("RANKED_SOLO_5x5", "Ranked Solo 5v5"),
      RANKED_TEAM_3x3 = new QueueType("RANKED_TEAM_3x3", "Ranked Team 3v3"),
      RANKED_TEAM_5x5 = new QueueType("RANKED_TEAM_5x5", "Ranked Team 5v5"),
      ONEFORALL_5x5 = new QueueType("ONEFORALL_5x5", "One for All 5v5"),
      FIRSTBLOOD_1x1 = new QueueType("FIRSTBLOOD_1x1", "Showdown 1v1"),
      FIRSTBLOOD_2x2 = new QueueType("FIRSTBLOOD_2x2", "Showdown 2v2"),
      SR_6x6 = new QueueType("SR_6x6", "Summoner's Rift Hexakill"),
      CAP_5x5 = new QueueType("CAP_5x5", "Teambuilder"),
      URF = new QueueType("URF", "URF"),
      URF_BOT = new QueueType("URF_BOT", "Co-op vs AI URF"),
      NIGHTMARE_BOT = new QueueType("NIGHTMARE_BOT", "Nightmare Bots"),
      ASCENSION = new QueueType("ASCENSION", "Ascension"),
      HEXAKILL = new QueueType("HEXAKILL", "Hexakill"),
      KING_PORO = new QueueType("KING_PORO", "King Poro"),
      COUNTER_PICK = new QueueType("COUNTER_PICK", "Nemesis Draft"),
      BILGEWATER = new QueueType("BILGEWATER", "Black Market Brawlers");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private QueueType(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class SpectatorState {
    public static readonly Dictionary<string, SpectatorState> Values = new Dictionary<string, SpectatorState>();

    public static readonly SpectatorState
      ALL = new SpectatorState("ALL", "All"),
      NONE = new SpectatorState("NONE", "None"),
      LOBBYONLY = new SpectatorState("LOBBYONLY", "Lobby Only"),
      DROPINONLY = new SpectatorState("DROPINONLY", "Friends Only");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private SpectatorState(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class GameMode {
    public static readonly Dictionary<string, GameMode> Values = new Dictionary<string, GameMode>();

    public static readonly GameMode
      CLASSIC = new GameMode("CLASSIC", "Classic"),
      ARAM = new GameMode("ARAM", "ARAM"),
      ODIN = new GameMode("ODIN", "Dominion"),
      TUTORIAL = new GameMode("TUTORIAL", "Tutorial"),
      ONEFORALL = new GameMode("ONEFORALL", "One for All"),
      ASCENSION = new GameMode("ASCENSION", "Ascension"),
      FIRSTBLOOD = new GameMode("FIRSTBLOOD", "Snowdown Showdown"),
      KINGPORO = new GameMode("KINGPORO", "King Poro");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private GameMode(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class ReportReason {
    public static readonly Dictionary<string, ReportReason> Values = new Dictionary<string, ReportReason>();

    public static readonly ReportReason
      NO_COMMUNICATION_WITH_TEAM = new ReportReason("NO_COMMUNICATION_WITH_TEAM", "Refusing To Communicate"),
      LEAVING_AFK = new ReportReason("LEAVING_AFK", "Leaving the Game or AFK"),
      NEGATIVE_ATTITUDE = new ReportReason("NEGATIVE_ATTITUDE", "Negative Attitude"),
      OFFENSIVE_LANGUAGE = new ReportReason("OFFENSIVE_LANGUAGE", "Offensive Language"),
      VERBAL_ABUSE = new ReportReason("VERBAL_ABUSE", "Verbal Abuse"),
      INAPPROPRIATE_NAME = new ReportReason("INAPPROPRIATE_NAME", "Inappropriate Name"),
      INTENTIONAL_FEEDING = new ReportReason("INTENTIONAL_FEEDING", "Intentionally Feeding"),
      ASSISTING_ENEMY = new ReportReason("ASSISTING_ENEMY", "Assisting Enemy Team"),
      SPAMMING = new ReportReason("SPAMMING", "Spamming"),
      UNSKILLED_PLAYER = new ReportReason("UNSKILLED_PLAYER", "Unskilled Player");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private ReportReason(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class RankedTier {
    public static readonly Dictionary<string, RankedTier> Values = new Dictionary<string, RankedTier>();

    public static readonly RankedTier
      BRONZE = new RankedTier("BRONZE", "Bronze"),
      SILVER = new RankedTier("SILVER", "Silver"),
      GOLD = new RankedTier("GOLD", "Gold"),
      PLATINUM = new RankedTier("PLATINUM", "Platinum"),
      DIAMOND = new RankedTier("DIAMOND", "Diamond"),
      CHALLENGER = new RankedTier("CHALLENGER", "Challenger"),
      MASTER = new RankedTier("MASTER", "Master");

    public string Key { get; private set; }
    public string Value { get; private set; }

		private RankedTier(string key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class GameConfig {
    public static readonly Dictionary<int, GameConfig> Values = new Dictionary<int, GameConfig>();

    public static readonly GameConfig
      Blind = new GameConfig(1, "Blind Pick"),
      Draft = new GameConfig(2, "Draft Pick"),
      DraftNoBan = new GameConfig(3, "No Ban Draft Pick"),
      AllRandom = new GameConfig(4, "All Random"),
      OpenPick = new GameConfig(5, "Open Pick"),
      BlindDraft = new GameConfig(7, "Blind Draft"),
      ITBlindPick = new GameConfig(11, "Infinite Time Blind Pick"),
      Cap = new GameConfig(12, "Team Builder"),
      OneForAll = new GameConfig(14, "One for All"),
      CrossDupe = new GameConfig(15, "Cross Dupe"),
      BlindDraftST = new GameConfig(16, "Blind Draft ST"),
      CounterPick = new GameConfig(17, "Counter Pick"),
      CapDraft = new GameConfig(18, "Team Builder Draft");

    public int Key { get; private set; }
    public string Value { get; private set; }

		private GameConfig(int key, string value) {
			Key = key; Value = value;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}

	public class ChatStatus {
    public static readonly Dictionary<string, ChatStatus> Values = new Dictionary<string, ChatStatus>();

    public static readonly ChatStatus
      championSelect = new ChatStatus("championSelect", "In Champion Select", 12),
      tutorial = new ChatStatus("tutorial", "Tutorial", 11),
      inGame = new ChatStatus("inGame", "In Game", 10),
      inQueue = new ChatStatus("inQueue", "In Queue", 9),
      spectating = new ChatStatus("spectating", "Spectating", 8),
      teamSelect = new ChatStatus("teamSelect", "In Team Select", 7),
      hostingNormalGame = new ChatStatus("hostingNormalGame", "Creating Normal Game", 6),
      hostingCoopVsAIGame = new ChatStatus("hostingCoopVsAIGame", "Creating Bot Game", 5),
      hostingRankedGame = new ChatStatus("hostingRankedGame", "Creating Ranked Game", 4),
      hostingPracticeGame = new ChatStatus("hostingPracticeGame", "Creating Custom Game", 3),
      inTeamBuilder = new ChatStatus("inTeamBuilder", "In Team Builder", 2),
      outOfGame = new ChatStatus("outOfGame", "Out of Game", 1);

    public string Key { get; private set; }
    public string Value { get; private set; }
    public int Priority { get; private set; }

		private ChatStatus(string key, string value, int priority) {
			Key = key; Value = value; Priority = priority;
			Values.Add(key, this);
		}

    public override string ToString() {
      return Value;
    }
	}
}
