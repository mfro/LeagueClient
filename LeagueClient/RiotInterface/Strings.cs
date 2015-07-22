using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Map = System.Collections.Generic.Dictionary<string, string>;

namespace LeagueClient.RiotInterface {
  public static class Strings {
    #region Chat Strings
    public static Map Queues = new Map {
		  {"URF", "URF"},
		  {"URF_BOT", "URF Co-op vs AI"},
		  {"CUSTOM", "Custom"},
		  {"NORMAL", "Normal 5v5"},
		  {"NORMAL_3x3", "Normal 3v3"},
		  {"ODIN_UNRANKED", "Dominion"},
		  {"ARAM_UNRANKED_5x5", "ARAM"},
		  {"BOT", "5v5 Co-op vs AI"},
		  {"BOT_3x3", "3v3 Co-op vs AI"},
		  {"RANKED_SOLO_5x5", "Ranked 5v5"},
		  {"RANKED_TEAM_3x3", "Ranked Team 3v3"},
		  {"RANKED_TEAM_5x5", "Ranked Team 5v5"},
		  {"ONEFORALL_5x5", "One for All"},
		  {"FIRSTBLOOD_1x1", "1v1 Snowdown"},
		  {"FIRSTBLOOD_2x2", "2v2 Snowdown"},
		  {"SR_6x6", "Summoner's Rift Hexakill"},
		  {"HEXAKILL", "Twisted Treeline Hexakill"},
		  {"CAP_5x5", "Teambuilder"},
		  {"NIGHTMAKRE_BOT", "Nightmare Bots"},
      {"ASCENSION", "Ascension"},
      {"KING_PORO", "King Poro"},
      {"COUNTER_PICK", "Nemesis Draft"},
		  {"NONE", "Custom"}
    };
    #endregion

    public static Map General = new Map {

    };

    #region Queues Maps and Game Modes
    public static Map GameModes = new Map {
      {"CLASSIC", "Classic"},
      {"ODIN", "Dominion"},
      {"ARAM", "ARAM"}
    };

    public static Dictionary<int, string> Maps = new MyMap {
      {"Summoner's Rift", 1, 2, 11},
      {"The Proving Grounds", 3},
      {"Twisted Treeline", 4, 10},
      {"The Crystal Scar", 8},
      {"Howling Abyss", 12}
    };

    public static Dictionary<int, string> QueueModes = new Dictionary<int, string> {
      {1, "Blind Pick"},
      {2, "Draft Pick"},
      {4, "All Random"},
      {12, "Team Builder"}
    };
    #endregion

    public static class ChampSelect {
      public static string
        YouSelect = "Your turn to select a champion!";
    }
  }

  public class MyMap : Dictionary<int, string> {
    public void Add(string value, params int[] keys) {
      foreach (var key in keys) Add(key, value);
    }
  }
}
