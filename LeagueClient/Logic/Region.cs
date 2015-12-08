using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class Region {
    public string MainServer { get; private set; }
    public string LoginQueueURL { get; private set; }
    public string ChatServer { get; private set; }

    public string UpdateBase { get; private set; }
    public string SolutionListing { get; private set; }
    public string GameListing { get; private set; }
    public string AirListing { get; private set; }

    public static readonly Region NA = new Region {
      MainServer = "prod.na2.lol.riotgames.com",
      LoginQueueURL = "https://lq.na2.lol.riotgames.com/",
      ChatServer = "chat.na2.lol.riotgames.com",

      UpdateBase = "http://l3cdn.riotgames.com/releases/live",
      SolutionListing = "http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA",
      AirListing = "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA",
      GameListing = "http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA",

    };
    public static readonly Region PBE = new Region {
      MainServer = "prod.pbe1.lol.riotgames.com",
      LoginQueueURL = "https://lq.pbe1.lol.riotgames.com/",
      ChatServer = "chat.pbe1.lol.riotgames.com"
    };
  }
}
