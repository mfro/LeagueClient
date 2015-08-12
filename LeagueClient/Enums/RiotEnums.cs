using System.Collections.Generic;

namespace LeagueClient.Logic {
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
      NONE = new QueueType("NONE", "None"),
      NORMAL = new QueueType("NORMAL", "Normal"),
      BOT = new QueueType("BOT", "Bot"),
      RANKED_SOLO_3x3 = new QueueType("RANKED_SOLO_3x3", "Ranked Solo 3v3"),
      RANKED_SOLO_5x5 = new QueueType("RANKED_SOLO_5x5", "Ranked Solo 5v5"),
      RANKED_PREMADE_3x3 = new QueueType("RANKED_PREMADE_3x3", "Ranked Teams 3v3"),
      RANKED_PREMADE_5x5 = new QueueType("RANKED_PREMADE_5x5", "Ranked Teams 5v5"),
      RANKED_SOLO_1x1 = new QueueType("RANKED_SOLO_1x1", "Ranked 1v1"),
      ODIN_UNRANKED = new QueueType("ODIN_UNRANKED", "Dominion"),
      ODIN_RANKED_SOLO = new QueueType("ODIN_RANKED_SOLO", "Ranked Solo Dominion"),
      ODIN_RANKED_TEAM = new QueueType("ODIN_RANKED_TEAM", "Ranked Teams Dominion"),
      RANKED_TEAM_3x3 = new QueueType("RANKED_TEAM_3x3", "Ranked Team 3v3"),
      RANKED_TEAM_5x5 = new QueueType("RANKED_TEAM_5x5", "Ranked Team 5v5"),
      NORMAL_3x3 = new QueueType("NORMAL_3x3", "Normal 3v3"),
      BOT_3x3 = new QueueType("BOT_3x3", "Co-op vs AI 3v3"),
      CAP_1x1 = new QueueType("CAP_1x1", "Teambuilder 1v1"),
      CAP_5x5 = new QueueType("CAP_5x5", "Teambuilder 5v5"),
      ARAM_UNRANKED_1x1 = new QueueType("ARAM_UNRANKED_1x1", "Aram 1v1"),
      ARAM_UNRANKED_2x2 = new QueueType("ARAM_UNRANKED_2x2", "Aram 2v2"),
      ARAM_UNRANKED_3x3 = new QueueType("ARAM_UNRANKED_3x3", "Aram 3v3"),
      ARAM_UNRANKED_5x5 = new QueueType("ARAM_UNRANKED_5x5", "Aram 5v5"),
      ARAM_UNRANKED_6x6 = new QueueType("ARAM_UNRANKED_6x6", "Aram 6v6"),
      ARAM_BOT = new QueueType("ARAM_BOT", "Co-op vs AI Aram"),
      ONEFORALL_5x5 = new QueueType("ONEFORALL_5x5", "One for All 5v5"),
      ONEFORALL_1x1 = new QueueType("ONEFORALL_1x1", "One for All 1v1"),
      FIRSTBLOOD_1x1 = new QueueType("FIRSTBLOOD_1x1", "Showdown 1v1"),
      FIRSTBLOOD_2x2 = new QueueType("FIRSTBLOOD_2x2", "Showdown 2v2"),
      SR_6x6 = new QueueType("SR_6x6", "Summoner's Rift Hexakill"),
      TT_5x5 = new QueueType("TT_5x5", "Twisted Treeline Hexakill"),
      URF = new QueueType("URF", "URF"),
      URF_BOT = new QueueType("URF_BOT", "Co-op vs AI URF"),
      FEATURED = new QueueType("FEATURED", "Featured"),
      FEATURED_BOT = new QueueType("FEATURED_BOT", "Co-op vs AI Featured"),
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

	public class GameConfig {
    public static readonly Dictionary<int, GameConfig> Values = new Dictionary<int, GameConfig>();

    public static readonly GameConfig
      Blind = new GameConfig(1, "Blind Pick"),
      Draft = new GameConfig(2, "Draft Pick"),
      DraftNoBan = new GameConfig(3, "Draft NoBan"),
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

}
