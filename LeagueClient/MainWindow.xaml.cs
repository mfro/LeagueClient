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
using RtmpSharp.Net;
using System.Security.Cryptography;
using LeagueClient.Logic;
using System.Windows.Threading;
using RtmpSharp.Messaging;
using System.Xml.Serialization;
using System.IO;
using LeagueClient.Logic.Settings;
using System.Threading;
using LeagueClient.UI;
using LeagueClient.UI.ChampSelect;
using LeagueClient.UI.Client;
using LeagueClient.UI.Login;
using RiotClient;
using RiotClient.Lobbies;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private ChampSelectPage champselect;
    private LandingPage landing;

    public MainWindow() {
      Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
      InitializeComponent();
      ((App) Application.Current).LoadResources();
      Loaded += (src, e) => Center();

      Session.Initialize();
      LoLClient.MainWindow = this;
      Start();
    }

    private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
      Session.Log("***UNHANDLED EXCEPTION***");
      Session.Log("***UNHANDLED EXCEPTION***");
      Session.Log("***UNHANDLED EXCEPTION***");
      Session.Log(e.Exception);
      Session.Log("***UNHANDLED EXCEPTION***");
      Session.Log("***UNHANDLED EXCEPTION***");
      Session.Log("***UNHANDLED EXCEPTION***");
    }

    public void Start() {
      ContentFrame.Content = new LoginPage();
    }

    public void LoginComplete() {
      var page = landing = new LandingPage();
      LoLClient.QueueManager = landing;
      ContentFrame.Content = page;

      if (Session.Current.LoginQueue.InGameCredentials?.InGame ?? false) {
        Session.Current.Credentials = Session.Current.LoginQueue.InGameCredentials;
        Session.Current.JoinGame();
        Session.Current.LoginQueue.InGameCredentials.InGame = false;
      }
    }

    public void Center() {
      double sWidth = SystemParameters.PrimaryScreenWidth;
      double sHeight = SystemParameters.PrimaryScreenHeight;
      Left = (sWidth / 2) - (Width / 2);
      Top = (sHeight / 2) - (Height / 2);
    }

    public void BeginChampionSelect(GameLobby game) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(BeginChampionSelect, game); return; }

      LoLClient.QueueManager.ShowPage(new InGamePage(true));
      champselect = new ChampSelectPage(game);

      champselect.ChampSelectCompleted += Champselect_ChampSelectCompleted;

      ContentFrame.Content = champselect;
      Session.Current.ChatManager.Status = ChatStatus.championSelect;
    }

    private void Champselect_ChampSelectCompleted(object sender, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(Champselect_ChampSelectCompleted, sender, e); return; }

      ShowLandingPage();

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
