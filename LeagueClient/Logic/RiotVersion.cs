using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class RiotVersion {
    public const string
      AirPath = @"RADS\projects\lol_air_client\releases",
      GamePath = @"RADS\projects\lol_game_client\releases",
      SolutionPath = @"RADS\solutions\lol_game_client_sln\releases";

    public Version AirVersion { get; set; }
    public Version GameVersion { get; set; }
    public Version SolutionVersion { get; set; }

    public List<RiotFile> AirFiles { get; } = new List<RiotFile>();

    private RiotVersion(Version air, Version game, Version solution, Region region) {
      AirVersion = air;
      GameVersion = game;
      SolutionVersion = solution;
      using (var web = new WebClient()) {
        var url = new Uri(region.UpdateBase, $"projects/lol_air_client/releases/{air}/packages/files/packagemanifest");
        var manifest = web.DownloadString(url);
        manifest = manifest.Replace("PKG1", "");
        var lines = manifest.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        AirFiles.AddRange(lines.Select(l => new RiotFile(l, region.UpdateBase)));
      }
    }

    public static RiotVersion GetInstalledVersion(Region region, string LeagueDir) {
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
      return new RiotVersion(airVersions.FirstOrDefault(), gameVersions.FirstOrDefault(), slnVersions.FirstOrDefault(), region);
    }

    public static async Task<RiotVersion> GetLatestVersion(Region region) {
      using (var web = new WebClient()) {
        var airList = await web.DownloadStringTaskAsync(region.AirListing);
        var gameList = await web.DownloadStringTaskAsync(region.GameListing);
        var solutionList = await web.DownloadStringTaskAsync(region.SolutionListing);

        Version airVersion, gameVersion, solutionVersion;
        Version.TryParse(airList.Split('\n').FirstOrDefault(), out airVersion);
        Version.TryParse(gameList.Split('\n').FirstOrDefault(), out gameVersion);
        Version.TryParse(solutionList.Split('\n').FirstOrDefault(), out solutionVersion);

        return new RiotVersion(airVersion, gameVersion, solutionVersion, region);
      }
    }
  }

  public class RiotFile {
    public Uri Url { get; }
    public string BIN { get; }
    public int Thing1 { get; }
    public int Thing2 { get; }
    public int Thing3 { get; }

    public RiotFile(string line, Uri root) {
      var bits = line.Split(',');
      Url = new Uri(root, bits[0].Substring(1));
      BIN = bits[1];
      Thing1 = int.Parse(bits[2]);
      Thing2 = int.Parse(bits[3]);
      Thing3 = int.Parse(bits[4]);
    }
  }
}
