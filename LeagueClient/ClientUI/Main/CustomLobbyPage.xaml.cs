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
using jabber.connection;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public partial class CustomLobbyPage : Page, IClientSubPage {

    private GameMap[] Maps = new[] { GameMap.SummonersRift, GameMap.ButchersBridge, GameMap.HowlingAbyss, GameMap.TheCrystalScar, GameMap.TheTwistedTreeline };
    private Room chatRoom;

    public CustomLobbyPage(GameDTO game) {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(ChatStatus.hostingPracticeGame);

      GotGameData(game);
    }

    public void GotGameData(GameDTO game) {
      if (chatRoom == null) {
        Dispatcher.Invoke(() => {
          MapLabel.Content = Maps.FirstOrDefault(m => m.MapId == game.MapId)?.DisplayName;
          ModeLabel.Content = GameMode.Values[game.GameMode];
          QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
          TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";

          chatRoom = Client.ChatManager.GetCustomRoom(game.RoomName, game.Id, game.RoomPassword);
          chatRoom.OnJoin += room => Dispatcher.Invoke(() => ShowLobbyMessage("Joined chat lobby"));
          chatRoom.OnParticipantJoin += (s, e) => Dispatcher.Invoke(() => ShowLobbyMessage(e.Nick + " has joined the lobby"));
          chatRoom.OnParticipantLeave += (s, e) => Dispatcher.Invoke(() => ShowLobbyMessage(e.Nick + " has left the lobby"));
          chatRoom.OnRoomMessage += (s, e) => Dispatcher.Invoke(() => ShowMessage(chatRoom.Participants[e.From].Nick, e.Body));
          Client.ChatManager.UpdateStatus(ChatStatus.inTeamBuilder);
          chatRoom.Join(game.RoomPassword);
        });
      }
      if (game.GameState.Equals("TEAM_SELECT")) {
        Dispatcher.Invoke(() => {
          BlueTeam.Children.Clear();
          RedTeam.Children.Clear();
          foreach (var player in game.TeamOne) {
            var control = new DefaultPlayer(player as PlayerParticipant);
            BlueTeam.Children.Add(control);
          }

          foreach (var player in game.TeamTwo) {
            var control = new DefaultPlayer(player as PlayerParticipant);
            RedTeam.Children.Add(control);
          }
        });
      } else if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {

      }
    }

    #region Chat

    private void SendMessage() {
      if (string.IsNullOrWhiteSpace(SendBox.Text)) return;
      chatRoom.PublicMessage(SendBox.Text);
      ShowMessage(Client.LoginPacket.AllSummonerData.Summoner.Name, SendBox.Text);
      SendBox.Text = "";
    }

    private void ShowLobbyMessage(string message) {
      var tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
      ChatScroller.ScrollToBottom();
    }

    private void ShowMessage(string user, string message) {
      var tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = user + ": ";
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CornflowerBlue);
      tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, App.FontBrush);
      ChatScroller.ScrollToBottom();
    }

    #endregion

    #region IClientSubPage

    public event EventHandler Close;

    public bool CanPlay() => false;
    public Page GetPage() => this;

    public void ForceClose() {
      RiotCalls.GameService.QuitGame();
    }

    public IQueuer HandleClose() {
      return new ReturnToLobbyQueuer(this);
    }

    #endregion

    private void InvokeGameData(GameDTO game) => Dispatcher.Invoke(() => GotGameData(game));

    private void Quit_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      ForceClose();
    }

    private bool collapsed;
    private void Collapse_Click(object sender, RoutedEventArgs e) {
      collapsed = !collapsed;
      foreach (var control in BlueTeam.Children)
        (control as DefaultPlayer).ForceExpand = collapsed;
      foreach (var control in RedTeam.Children)
        (control as DefaultPlayer).ForceExpand = collapsed;
      CollapseButt.Content = collapsed ? "Expand" : "Collapse";
    }

    private void Invite_Click(object sender, RoutedEventArgs e) {

    }

    private void Send_Click(object sender, RoutedEventArgs e) {
      SendMessage();
    }
  }
}
