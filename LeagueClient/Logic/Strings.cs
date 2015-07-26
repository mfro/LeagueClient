using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Map = System.Collections.Generic.Dictionary<string, string>;

namespace LeagueClient.Logic {
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
      {3, "Draft NoBan"},
      {4, "All Random"},
      {5, "Simultaneous"},
      {6, "Draft Tournament"},
      {7, "Simultaneous TD"},
      {10, "Blind Random"},
      {11, "Advanced Tutorial"},
      {12, "Team Builder"},
      {13, "Blind Random"},
      {14, "Blind Dupe"},
      {15, "Cross Dupe"},
      {16, "Blind Draft ST"},
      {17, "Counter Pick"},
      {18, "Team Builder Draft"}
    };
    #endregion

    public class Position {
      public static Dictionary<string, Position> Values = new Dictionary<string, Position>();

      public string Id { get; set; }
      public string Value { get; set; }

      public static readonly Position
        TOP = new Position("Top Lane", "TOP"),
        JUNGLE = new Position("Jungle", "JUNGLE"),
        MIDDLE = new Position("Middle Lane", "MIDDLE"),
        BOTTOM = new Position("Bottom Lane", "BOTTOM");

      private Position(string value, string key) {
        Id = key; Value = value;
        Values.Add(Id, this);
      }
    }

    public class Role {
      public static Dictionary<string, Role> Values = new Dictionary<string, Role>();

      public string Id { get; set; }
      public string Value { get; set; }

      public static readonly Role
        SUPPORT = new Role("Support", "SUPPORT"),
        MAGE = new Role("Mage", "MAGE"),
        MARKSMAN = new Role("Marksman", "MARKSMAN"),
        ASSASSIN = new Role("Assassin", "ASSASSIN"),
        FIGHTER = new Role("Fighter", "FIGHTER"),
        TANK = new Role("Tank", "TANK");

      private Role(string value, string key) {
        Id = key; Value = value;
        Values.Add(Id, this);
      }
    }

    public class ChampSelect {
      public const string
        YouSelect = "Your turn to select a champion!";
    }
  }

  public class MyMap : Dictionary<int, string> {
    public void Add(string value, params int[] keys) {
      foreach (var key in keys) Add(key, value);
    }
  }
}
