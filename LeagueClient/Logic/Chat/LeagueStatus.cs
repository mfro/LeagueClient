using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LeagueClient.Logic.Chat {
  public class LeagueStatus {
    private const string StatusTemplate =
      "<body>" +
        "<level>30</level>" +
        "<profileIcon>5</profileIcon>" +
        "<statusMsg>{0}</statusMsg>" +
        "<gameStatus>{1}</gameStatus>" +
      "</body>";
    public int ProfileIcon { get; private set; }
    public long TimeStamp { get; private set; }
    public string Message { get; private set; }
    public string Champion { get; private set; }

    public GameStatus GameStatus { get; private set; }
    public StatusShow Show { get; private set; }

    public LeagueStatus(string message, GameStatus ingame) {
      Message = message;
      GameStatus = ingame;
    }

    public LeagueStatus(string status, string show) {
      var doc = new XmlDocument();
      doc.LoadXml(status);
      foreach (XmlNode node in doc.SelectNodes("body/*")) {
        switch (node.Name) {
          case "profileIcon":
            ProfileIcon = int.Parse(node.InnerText);
            break;
          case "statusMsg":
            Message = node.InnerText;
            break;
          case "gameStatus":
            GameStatus = GetGameStatus(node.InnerText);
            break;
          case "timeStamp":
            TimeStamp = long.Parse(node.InnerText);
            break;
          case "skinname":
            Champion = node.InnerText;
            break;
        }
      }
      Show = (StatusShow) Enum.Parse(typeof(StatusShow), show, true);
    }

    public string ToXML() {
      return string.Format(StatusTemplate, Message, GameStatus.Id);
    }

    #region GameStatus and GameType
    public static GameStatus
      ChampSelect = new GameStatus("championSelect", "In Champion Select", 12),
      Tutorial = new GameStatus("tutorial", "Tutorial", 11),
      InGame = new GameStatus("inGame", "In Game", 10),
      InQueue = new GameStatus("inQueue", "In Queue", 9),
      Spectating = new GameStatus("spectating", "Spectating", 8),
      TeamSelect = new GameStatus("teamSelect", "In Team Select", 7),
      CreatingNormal = new GameStatus("hostingNormalGame", "Creating Normal Game", 6),
      CreatingBots = new GameStatus("hostingCoopVsAIGame", "Creating Bot Game", 5),
      CreatingRanked = new GameStatus("hostingRankedGame", "Creating Ranked Game", 4),
      CreatingCustom = new GameStatus("hostingPracticeGame", "Creating Custom Game", 3),
      InTeamBuilder = new GameStatus("inTeamBuilder", "In Team Builder", 2),
      Idle = new GameStatus("outOfGame", "Out of Game", 1);

    public static GameStatus GetGameStatus(string text) {
      switch (text) {
        case "championSelect": return ChampSelect;
        case "tutorial": return Tutorial;
        case "inGame": return InGame;
        case "inQueue": return InQueue;
        case "spectating": return Spectating;
        case "teamSelect": return TeamSelect;
        case "hostingNormalGame": return CreatingNormal;
        case "hostingCoopVsAIGame": return CreatingBots;
        case "hostingRankedGame": return CreatingRanked;
        case "hostingPracticeGame": return CreatingCustom;
        case "inTeamBuilder": return InTeamBuilder;
        default: return Idle;
      }
    }
    #endregion
  }

  public enum StatusShow {
    Chat,
    Dnd,
    Away
  }

  public class GameStatusLookup : Dictionary<string, GameStatus> {
    public void Add(GameStatus status) {
      Add(status.Id, status);
    }
  }

  public class GameStatus {
    public string Id { get; set; }
    public string Name { get; set; }
    public int Priority { get; set; }

    public GameStatus(string id, string name, int prio) {
      Id = id; Name = name; Priority = prio;
    }
  }
}
