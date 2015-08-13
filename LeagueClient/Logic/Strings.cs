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
    public static Dictionary<int, string> Maps = new Dictionary<int, string> {
      {"Summoner's Rift", 1, 2, 11},
      {"The Proving Grounds", 3},
      {"Twisted Treeline", 4, 10},
      {"The Crystal Scar", 8},
      {"Howling Abyss", 12}
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
