using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient {
  public class Region {
    public string MainServer { get; private set; }
    public string ChatServer { get; private set; }
    public string Platform { get; private set; }

    public string NewsURL { get; private set; }
    public string LoginQueueURL { get; private set; }

    public Uri UpdateBase { get; private set; }
    public Uri AirListing { get; private set; }
    public Uri GameListing { get; private set; }
    public Uri SolutionListing { get; private set; }

    public static readonly Region NA = new Region {
      MainServer = "prod.na2.lol.riotgames.com",
      LoginQueueURL = "https://lq.na2.lol.riotgames.com/",
      Platform = "NA1",

      NewsURL = "http://na.leagueoflegends.com/en/rss.xml",
      ChatServer = "chat.na2.lol.riotgames.com",

      UpdateBase = new Uri("http://l3cdn.riotgames.com/releases/live/"),
      AirListing = new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA"),
      GameListing = new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_game_client/releases/releaselisting_NA"),
      SolutionListing = new Uri("http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA"),

    };

    public static readonly Region EUW = new Region {
      MainServer = "prod.euw1.lol.riotgames.com",
      LoginQueueURL = "https://lq.euw1.lol.riotgames.com/",
      Platform = "EUW1",

      NewsURL = "http://euw.leagueoflegends.com/en/rss.xml",
      ChatServer = "chat.euw1.lol.riotgames.com",

      UpdateBase = new Uri("http://l3cdn.riotgames.com/releases/live/"),
      AirListing = new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_EUW"),
      GameListing = new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_game_client/releases/releaselisting_EUW"),
      SolutionListing = new Uri("http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_EUW"),

    };

    public static readonly Region PBE = new Region {
      MainServer = "prod.pbe1.lol.riotgames.com",
      LoginQueueURL = "https://lq.pbe1.lol.riotgames.com/",
      ChatServer = "chat.pbe1.lol.riotgames.com",
      Platform = "PBE1",
    };
  }
}
