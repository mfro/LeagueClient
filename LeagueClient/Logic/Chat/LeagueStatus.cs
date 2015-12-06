using agsXMPP.protocol.client;
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
        "<profileIcon>5</profileIcon>" +
        "<level>30</level>" +
        "<gameStatus>{1}</gameStatus>" +
        "<statusMsg>{0}</statusMsg>" +
      "</body>";
    public int ProfileIcon { get; private set; }
    public long TimeStamp { get; private set; }
    public string Message { get; private set; }
    public string Champion { get; private set; }

    public string Raw { get; private set; }

    public ChatStatus GameStatus { get; private set; }
    public ShowType Show { get; private set; }

    public LeagueStatus(string message, ChatStatus ingame) {
      Message = message;
      GameStatus = ingame;
    }

    public LeagueStatus(string status, ShowType show) {
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
      Show = show;
    }

    public string ToXML() {
      return string.Format(StatusTemplate, Message, GameStatus.Key);
    }
  }
}
