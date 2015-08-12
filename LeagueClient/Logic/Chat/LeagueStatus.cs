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

    public string Raw { get; private set; }

    public ChatStatus GameStatus { get; private set; }
    public StatusShow Show { get; private set; }

    public LeagueStatus(string message, ChatStatus ingame) {
      Message = message;
      GameStatus = ingame;
    }

    public LeagueStatus(string status, string show) {
      var doc = new XmlDocument();
      doc.LoadXml(status);
      Raw = status;
      foreach (XmlNode node in doc.SelectNodes("body/*")) {
        switch (node.Name) {
          case "profileIcon":
            ProfileIcon = int.Parse(node.InnerText);
            break;
          case "statusMsg":
            Message = node.InnerText;
            break;
          case "gameStatus":
            GameStatus = ChatStatus.Values[node.InnerText];
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
      return string.Format(StatusTemplate, Message, GameStatus.Key);
    }
  }

  public enum StatusShow {
    Chat,
    Dnd,
    Away
  }
}
