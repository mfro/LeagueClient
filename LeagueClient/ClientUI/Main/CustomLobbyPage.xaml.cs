using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public GameDTO GameDto { get; private set; }

    private ChatRoomController chatRoom;

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
      chatRoom = new ChatRoomController(SendBox, ChatHistory, ChatSend, ChatScroller);
      Client.ChatManager.UpdateStatus(ChatStatus.teamSelect);
    }

    #endregion

    public bool HandleMessage(MessageReceivedEventArgs e) {
      GameDTO game;
      LobbyStatus status;
      InvitePrivileges invite;
      if ((game = e.Body as GameDTO) != null) {
        Dispatcher.MyInvoke(GotGameData, game);
        return true;
      } else if((status = e.Body as LobbyStatus) != null) {
        Dispatcher.MyInvoke(GotLobbyStatus, status);
        return true;
      } else if((invite = e.Body as InvitePrivileges) != null) {
        Dispatcher.Invoke(() => InviteButt.Visibility = invite.canInvite ? Visibility.Visible : Visibility.Collapsed);
      }
      return false;
    }

    public void GotLobbyStatus(LobbyStatus status) {
      InviteList.Children.Clear();

      foreach (var player in status.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
        InviteList.Children.Add(new InvitedPlayer(player));
      }
    }

    public void GotGameData(GameDTO game) {
      GameDto = game;
      if (!chatRoom.IsJoined) {
        chatRoom.JoinChat(RiotChat.GetCustomRoom(game.Name, game.Id, game.RoomPassword), game.RoomPassword);

        StartButt.Visibility = (game.OwnerSummary.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId) ? Visibility.Visible : Visibility.Collapsed;

        Dispatcher.Invoke(() => {
          var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.MapId);

          MapImage.Source = GameMap.Images[map];
          MapLabel.Content = map.DisplayName;
          ModeLabel.Content = GameMode.Values[game.GameMode];
          QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
          TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";
        });
      }
      if (game.GameState.Equals("TEAM_SELECT")) {
        Dispatcher.Invoke(() => {
          BlueTeam.Children.Clear();
          RedTeam.Children.Clear();
          ObserverList.Children.Clear();
          GameNameLabel.Content = game.Name;

          if (game.OwnerSummary.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId)
            InviteButt.Visibility = Visibility.Visible;

          foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
            var player = thing as PlayerParticipant;
            bool blue = game.TeamOne.Contains(player);
            var bot = thing as BotParticipant;
            UserControl control;
            if (player != null)
              control = new LobbyPlayer(player, true);
            else if (bot != null)
              control = new LobbyBotPlayer(bot, true);
            else throw new NotImplementedException(thing.GetType().Name);

            Dispatcher.BeginInvoke((Action) (() => {
              BlueTeam.Height = RedTeam.Height = control.ActualHeight * (game.MaxNumPlayers / 2);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
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

          foreach(var thing in game.Observers) {
            ObserverList.Children.Add(new Label { Content = thing.SummonerName });
          }
        });
      } else if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        Close?.Invoke(this, new EventArgs());
        Client.MainWindow.BeginChampSelect(game);
      }
    }

    #region IClientSubPage

    public event EventHandler Close;

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => true;

    public void ForceClose() {
      RiotCalls.GameService.QuitGame();
      Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
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

    //private bool collapsed;
    //private void Collapse_Click(object sender, RoutedEventArgs e) {
    //  collapsed = !collapsed;
    //  foreach (var control in BlueTeam.Children)
    //    (control as ICollapsable).ForceExpand = !collapsed;
    //  foreach (var control in RedTeam.Children)
    //    (control as ICollapsable).ForceExpand = !collapsed;
    //  CollapseButt.Content = collapsed ? "Expand Players" : "Collapse Players";
    //}

    private void Invite_Click(object sender, RoutedEventArgs e) => InvitePopup.BeginStoryboard(App.FadeIn);

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
      RiotCalls.GameService.SwitchTeams(GameDto.Id);
      RedJoin.Visibility = Visibility.Collapsed;
      BlueJoin.Visibility = Visibility.Visible;
    }

    private void BlueJoin_Click(object sender, RoutedEventArgs e) {
      RiotCalls.GameService.SwitchTeams(GameDto.Id);
      RedJoin.Visibility = Visibility.Visible;
      BlueJoin.Visibility = Visibility.Collapsed;
    }

    private void Spectate_Click(object sender, RoutedEventArgs e) {
      RiotCalls.GameService.SwitchPlayerToObserver(GameDto.Id);
    }

    private void Start_Click(object sender, RoutedEventArgs e) {
      RiotCalls.GameService.StartChampionSelection(GameDto.Id, GameDto.OptimisticLock);
    }

    #endregion
  }
}
