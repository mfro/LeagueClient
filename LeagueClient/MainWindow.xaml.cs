using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MFroehlich.League.Assets;
using LeagueClient.ClientUI;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Net;
using System.Security.Cryptography;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using System.Windows.Threading;
using RtmpSharp.Messaging;
using LeagueClient.ClientUI.Main;
using System.Xml.Serialization;
using System.IO;
using LeagueClient.Logic.Settings;
using System.Threading;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private IClientPage currentPage;

    private ChampSelectPage champselect;
    private LandingPage landing;

    public MainWindow() {
      Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
      InitializeComponent();
      ((App) Application.Current).LoadResources();
      Loaded += (src, e) => Center();

      Client.Initialize();
      Client.MainWindow = this;
      Start();
    }

    private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
      Client.Log("***UNHANDLED EXCEPTION***");
      Client.Log("***UNHANDLED EXCEPTION***");
      Client.Log("***UNHANDLED EXCEPTION***");
      Client.Log(e.Exception);
      Client.Log("***UNHANDLED EXCEPTION***");
      Client.Log("***UNHANDLED EXCEPTION***");
      Client.Log("***UNHANDLED EXCEPTION***");
    }

    public void Start() {
      ContentFrame.Content = new LoginPage();
    }

    public void LoginComplete() {
      var page = landing = new LandingPage();
      Client.Session.QueueManager = landing;
      ContentFrame.Content = page;
      currentPage = page;

      if (Client.Session.LoginQueue.InGameCredentials?.InGame ?? false) {
        Client.Session.Credentials = Client.Session.LoginQueue.InGameCredentials;
        Client.Session.JoinGame();
        Client.Session.LoginQueue.InGameCredentials.InGame = false;
      }
    }

    public bool HandleMessage(MessageReceivedEventArgs e) {
      return currentPage?.HandleMessage(e) ?? false;
    }

    public void Center() {
      double sWidth = SystemParameters.PrimaryScreenWidth;
      double sHeight = SystemParameters.PrimaryScreenHeight;
      Left = (sWidth / 2) - (Width / 2);
      Top = (sHeight / 2) - (Height / 2);
    }

    public void BeginChampionSelect(GameDTO game) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(BeginChampionSelect, game); return; }

      Client.Session.QueueManager.ShowPage(new InGamePage(true));
      champselect = new ChampSelectPage(game);

      currentPage = champselect;
      champselect.ChampSelectCompleted += Champselect_ChampSelectCompleted;

      ContentFrame.Content = champselect;
      Client.Session.ChatManager.Status = ChatStatus.championSelect;
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
    }

    private void Champselect_ChampSelectCompleted(object sender, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(Champselect_ChampSelectCompleted, sender, e); return; }

      ShowLandingPage();

      currentPage = landing;
      champselect.ChampSelectCompleted -= Champselect_ChampSelectCompleted;
      champselect = null;
    }

    public void ShowLandingPage() {
      ContentFrame.Content = landing;
    }

    public void ShowChampSelect() {
      ContentFrame.Content = champselect;
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) {
      WindowState = WindowState.Minimized;
    }
  }
}
