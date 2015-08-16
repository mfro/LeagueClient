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
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public partial class CustomLobbyPage : Page, IClientSubPage {

    public double GameId { get; private set; }

    private GameMap[] Maps = new[] { GameMap.SummonersRift, GameMap.ButchersBridge, GameMap.HowlingAbyss, GameMap.TheCrystalScar, GameMap.TheTwistedTreeline };
    private Room chatRoom;

    #region Constructors

    public CustomLobbyPage(GameDTO game) {
      SharedInit();

      GotGameData(game);
    }

    public CustomLobbyPage() {
      SharedInit();
    }

    private void SharedInit() {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(ChatStatus.hostingPracticeGame);
    }

    #endregion

    public bool HandleMessage(MessageReceivedEventArgs e) {
      GameDTO game;
      LobbyStatus status;
      if ((game = e.Body as GameDTO) != null) {
        Client.Invoke(GotGameData, game);
        return true;
      } else if((status = e.Body as LobbyStatus) != null) {
        Client.Invoke(GotLobbyStatus, status);
        return true;
      } else {

      }
      return false;
    }

    public void GotLobbyStatus(LobbyStatus status) {
      InviteList.Children.Clear();

      foreach (var player in status.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
        var grid = new Grid();
        var name = new Label { Content = player.SummonerName };
        var state = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        switch (player.InviteeState) {
          case "PENDING": state.Content = "Pending"; break;
          case "ACCEPTED": state.Content = "Accepted"; break;
          case "QUIT": state.Content = "Quit"; break;
          default: break;
        }
        grid.Children.Add(name);
        grid.Children.Add(state);
        InviteList.Children.Add(grid);
      }
    }

    public void GotGameData(GameDTO game) {
      GameId = game.Id;
      if (chatRoom == null) {
        Dispatcher.Invoke(() => {
          var map = Maps.FirstOrDefault(m => m.MapId == game.MapId);
          switch (map.MapId) {
            case 8: MapImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/CScarImage.png"));   break;
            case 10: MapImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/TTImage.png"));     break;
            case 11: MapImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/SRiftImage.png"));  break;
            case 12: MapImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/HAbyssImage.png")); break;
            case 14: MapImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Bilgewater.png"));  break;
          }
          MapLabel.Content = map.DisplayName;
          ModeLabel.Content = GameMode.Values[game.GameMode];
          QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
          TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";

          chatRoom = Client.ChatManager.GetCustomRoom(game.Name, game.Id, game.RoomPassword);
          chatRoom.OnJoin += room => Dispatcher.Invoke(() => ShowLobbyMessage("Joined chat lobby"));
          chatRoom.OnParticipantJoin += (s, e) => Dispatcher.Invoke(() => ShowLobbyMessage(e.Nick + " has joined the lobby"));
          chatRoom.OnParticipantLeave += (s, e) => Dispatcher.Invoke(() => ShowLobbyMessage(e.Nick + " has left the lobby"));
          chatRoom.OnRoomMessage += (s, e) => Dispatcher.Invoke(() => ShowMessage(chatRoom.Participants[e.From].Nick, e.Body));
          Client.ChatManager.UpdateStatus(ChatStatus.hostingPracticeGame);
          chatRoom.Join(game.RoomPassword);
        });
      }
      if (game.GameState.Equals("TEAM_SELECT")) {
        Dispatcher.Invoke(() => {
          BlueTeam.Children.Clear();
          RedTeam.Children.Clear();
          foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
            var player = thing as PlayerParticipant;
            bool blue = game.TeamOne.Contains(player);
            var bot = thing as BotParticipant;
            UIElement control;
            if (player != null)
              control = new LobbyPlayer(player, !collapsed);
            else if (bot != null)
              control = new LobbyBotPlayer(bot, !collapsed);
            else throw new NotImplementedException(thing.GetType().Name);

            if (blue) {
              BlueTeam.Children.Add(control);
              if (player?.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId) {
                RedJoin.Visibility = Visibility.Visible;
                BlueJoin.Visibility = Visibility.Collapsed;
              }
            } else {
              RedTeam.Children.Add(control);
              if (player?.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId) {
                RedJoin.Visibility = Visibility.Collapsed;
                BlueJoin.Visibility = Visibility.Visible;
              }
            }
          }
        });
      } else if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        ForceClose();
        Client.MainWindow.BeginChampSelect(game);
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

    public Page Page => this;
    public bool CanPlay => false;

    public void ForceClose() {
      RiotCalls.GameService.QuitGame();
    }

    public IQueuer HandleClose() {
      return new ReturnToLobbyQueuer(this);
    }

    #endregion

    #region UI Events

    private void Quit_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      ForceClose();
    }

    private bool collapsed;
    private void Collapse_Click(object sender, RoutedEventArgs e) {
      collapsed = !collapsed;
      foreach (var control in BlueTeam.Children)
        (control as ICollapsable).ForceExpand = !collapsed;
      foreach (var control in RedTeam.Children)
        (control as ICollapsable).ForceExpand = !collapsed;
      CollapseButt.Content = collapsed ? "Expand Players" : "Collapse Players";
    }

    private void Invite_Click(object sender, RoutedEventArgs e) => InvitePopup.BeginStoryboard(App.FadeIn);

    private void Send_Click(object sender, RoutedEventArgs e) => SendMessage();

    private void InvitePopup_Close(object sender, EventArgs e) {
      InvitePopup.BeginStoryboard(App.FadeOut);
      foreach (var user in InvitePopup.Users.Where(u => u.Value)) {
        double id;
        if (double.TryParse(user.Key.Replace("sum", ""), out id)) {
          RiotCalls.GameInvitationService.Invite(id);
        } else Client.TryBreak("Cannot parse user " + user.Key);
      }
    }

    private void RedJoin_Click(object sender, RoutedEventArgs e) {
      RiotCalls.GameService.SwitchTeams(GameId);
      RedJoin.Visibility = Visibility.Collapsed;
      BlueJoin.Visibility = Visibility.Visible;
    }

    private void BlueJoin_Click(object sender, RoutedEventArgs e) {
      RiotCalls.GameService.SwitchTeams(GameId);
      RedJoin.Visibility = Visibility.Visible;
      BlueJoin.Visibility = Visibility.Collapsed;
    }

    #endregion
  }
}
