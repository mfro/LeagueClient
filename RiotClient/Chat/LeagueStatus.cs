using agsXMPP.protocol.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RiotClient.Chat {
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
      var unranked = Session.Current.Account.LoginPacket.PlayerStatSummaries.PlayerStatSummarySet.FirstOrDefault(x => x.PlayerStatSummaryTypeString.Equals("Unranked"));
      var ranked = Session.Current.Account.LoginPacket.PlayerStatSummaries.PlayerStatSummarySet.FirstOrDefault(x => x.PlayerStatSummaryTypeString.Equals("RankedSolo5x5"));
      var league = Session.Current.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));

      var dict = new Dictionary<string, object>();
      dict["profileIcon"] = Session.Current.Account.ProfileIconID;
      dict["level"] = Session.Current.Account.Level;
      dict["wins"] = unranked?.Wins ?? 0;
      dict["gameStatus"] = GameStatus.Key;

      if (!string.IsNullOrEmpty(Message)) dict["statusMsg"] = Message;

      if (ranked != null && league != null) {
        dict["rankedLeagueName"] = league.DivisionName;
        dict["rankedLeagueDivision"] = league.Rank;
        dict["rankedLeagueTier"] = league.Tier;
        dict["rankedLeagueQueue"] = league.Queue;
        dict["rankedWins"] = ranked.Wins;
      }

      var xml = new XElement("body", dict.Select(pair => new XElement(pair.Key, pair.Value)));
      return xml.ToString();
    }
  }
}
