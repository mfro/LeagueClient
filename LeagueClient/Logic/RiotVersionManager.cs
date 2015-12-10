using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class RiotVersionManager {
    public const string
      AirPath = @"RADS\projects\lol_air_client\releases",
      GamePath = @"RADS\projects\lol_game_client\releases",
      SolutionPath = @"RADS\solutions\lol_game_client_sln\releases";

    public Version AirVersion { get; set; }
    public Version GameVersion { get; set; }
    public Version SolutionVersion { get; set; }

    public static RiotVersionManager FetchInstalled(string LeagueDir) {
      var airInstalled = Directory.EnumerateDirectories(Path.Combine(LeagueDir, AirPath));
      var airVersions = from dir in airInstalled
                        select Version.Parse(Path.GetFileName(dir)) into v
                        orderby v descending
                        select v;
      var gameInstalled = Directory.EnumerateDirectories(Path.Combine(LeagueDir, GamePath));
      var gameVersions = from dir in gameInstalled
                         select Version.Parse(Path.GetFileName(dir)) into v
                         orderby v descending
                         select v;
      var slnInstalled = Directory.EnumerateDirectories(Path.Combine(LeagueDir, SolutionPath));
      var slnVersions = from dir in slnInstalled
                        select Version.Parse(Path.GetFileName(dir)) into v
                        orderby v descending
                        select v;
      return new RiotVersionManager {
        AirVersion = airVersions.FirstOrDefault(),
        GameVersion = gameVersions.FirstOrDefault(),
        SolutionVersion = slnVersions.FirstOrDefault()
      };
    }

    public static async Task<RiotVersionManager> FetchLatest(Region region) {
      using (var web = new WebClient()) {
        var airList = await web.DownloadStringTaskAsync(region.AirListing);
        var gameList = await web.DownloadStringTaskAsync(region.GameListing);
        var solutionList = await web.DownloadStringTaskAsync(region.SolutionListing);

        Version airVersion, gameVersion, solutionVersion;
        Version.TryParse(airList.Split('\n').FirstOrDefault(), out airVersion);
        Version.TryParse(gameList.Split('\n').FirstOrDefault(), out gameVersion);
        Version.TryParse(solutionList.Split('\n').FirstOrDefault(), out solutionVersion);

        return new RiotVersionManager { AirVersion = airVersion, GameVersion = gameVersion, SolutionVersion = solutionVersion };
      }
    }
  }
}
