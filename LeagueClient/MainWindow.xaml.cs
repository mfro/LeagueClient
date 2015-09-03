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

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private IClientPage currentPage;

    private ClientUI.Main.ClientPage mainPage;

    public MainWindow() {
      Client.Log("Pre-Init");
      Client.PreInitialize(this);
      Client.Log(LeagueData.CurrentVersion);

      InitializeComponent();
      ((App) App.Current).LoadResources();
      if (!PatcherPage.NeedsPatch()) {
        PatchComplete();
      } else {
        ContentFrame.Content = new PatcherPage();
      }
    }

    public void LoginComplete() {
      var page = mainPage = new ClientUI.Main.ClientPage();
      Client.QueueManager = page;
      ContentFrame.Content = page;
      this.currentPage = page;
    }

    public void PatchComplete() {
      ContentFrame.Content = new LoginPage();
    }

    public bool HandleMessage(MessageReceivedEventArgs e) {
      return currentPage?.HandleMessage(e) ?? false;
    }

    public void Center() {
      double sWidth = SystemParameters.PrimaryScreenWidth;
      double sHeight = SystemParameters.PrimaryScreenHeight;
      Client.MainWindow.Left = (sWidth / 2) - (Client.MainWindow.Width / 2);
      Client.MainWindow.Top = (sHeight / 2) - (Client.MainWindow.Height / 2);
    }

    public void BeginChampSelect(GameDTO game) {
      var page = new ChampSelectPage(game);
      currentPage = page;
      ContentFrame.Content = page;
      Client.ChatManager.UpdateStatus(ChatStatus.championSelect);
      RiotCalls.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
    }

    public void ShowInGamePage() {
      currentPage = mainPage;
      ContentFrame.Content = mainPage;
      mainPage.ShowPage(new InGamePage());
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) {
      WindowState = WindowState.Minimized;
    }
  }
}
