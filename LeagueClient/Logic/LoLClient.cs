using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using MFroehlich.League;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;
using LeagueClient.Logic.Settings;
using System.Xml.Serialization;
using System.Net;
using System.Text;
using LeagueClient.UI.Selectors;
using LeagueClient.UI.Client;
using System.Windows;
using RiotClient;

namespace LeagueClient.Logic {
  public class LoLClient {
    internal static IQueueManager QueueManager { get; set; }

    internal static Strings Strings => Strings.Localizations[Session.Locale];
    internal static MainWindow MainWindow { get; set; }
    internal static PopupSelector PopupSelector { get; set; }

    public static void ShowPopup(PopupSelector.Selector thing) {
      PopupSelector.CurrentSelector = thing;
      PopupSelector.BeginStoryboard(App.FadeIn);
    }

    public static void HidePopup() {
      PopupSelector.BeginStoryboard(App.FadeOut);
    }
  }
}