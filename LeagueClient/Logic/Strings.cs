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
    public static Map General = new Map {

    };

    #region Queues Maps and Game Modes
    public static Map GameModes = new Map {
      {"CLASSIC", "Classic"},
      {"ODIN", "Dominion"},
      {"ARAM", "ARAM"}
    };

    public static Dictionary<int, string> Maps = new Dictionary<int, string> {
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

    public class ChampSelect {
      public const string
        YouSelect = "Your turn to select a champion!";
    }

    public static void Add(this Dictionary<int, string> dict, string value, params int[] keys) {
      foreach (var key in keys) dict.Add(key, value);
    }
  }
}
