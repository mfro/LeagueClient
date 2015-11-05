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
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for DefaultLobbyPage.xaml
  /// </summary>
  public partial class DefaultLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private MatchMakerParams mmp;
    private GameQueueConfig config;
    private ChatRoomController chatRoom;

    public DefaultLobbyPage(MatchMakerParams mmp) {
      InitializeComponent();

      this.mmp = mmp;
      this.chatRoom = new ChatRoomController(SendBox, ChatHistory, ChatSend, ChatScroller);
      Client.ChatManager.UpdateStatus(ChatStatus.hostingNormalGame);

      InviteButton.Visibility = Visibility.Hidden;

      config = Client.AvailableQueues[mmp.QueueIds[0]];
      var map = GameMap.Maps.FirstOrDefault(m => config.SupportedMapIds.Contains(m.MapId));

      MapImage.Source = GameMap.Images[map];
      MapLabel.Content = map.DisplayName;
      QueueLabel.Content = GameConfig.Values[config.GameTypeConfigId].Value;
      ModeLabel.Content = config.Ranked ? "Ranked" : ModeLabel.Content = GameMode.Values[config.GameMode].Value;
      TeamSizeLabel.Content = $"{config.NumPlayersPerTeam}v{config.NumPlayersPerTeam}";
    }

    #region RTMP Messages
    public bool HandleMessage(MessageReceivedEventArgs args) {
      var lobby = args.Body as LobbyStatus;
      var invite = args.Body as InvitePrivileges;
      var queue = args.Body as SearchingForMatchNotification;

      if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        Dispatcher.Invoke(() => InviteButton.Visibility = invite.canInvite ? Visibility.Visible : Visibility.Collapsed);
      } else if (queue != null) {
        //TODO starting premade queues
      }

      return false;
    }

    public void GotLobbyStatus(LobbyStatus lobby) {
      if (!chatRoom.IsJoined)
        chatRoom.JoinChat(RiotChat.GetLobbyRoom(lobby.InvitationID, lobby.ChatKey), lobby.ChatKey);

      Dispatcher.Invoke(() => {
        InviteList.Children.Clear();
        foreach (var player in lobby.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
          InviteList.Children.Add(new InvitedPlayer(player));
        }
        PlayerList.Children.Clear();
        foreach (var player in lobby.PlayerIds) {
          var control = new LobbyPlayer(player, true);
          PlayerList.Children.Add(control);
          Dispatcher.BeginInvoke((Action) (() => QueueDetailsGrid.Height = PlayerList.Height = control.ActualHeight * config.NumPlayersPerTeam),
            System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        if (lobby.Owner.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId)
          StartButton.Visibility = InviteButton.Visibility = Visibility.Visible;
        else StartButton.Visibility = Visibility.Hidden;
      });
    }
    #endregion

    #region UI Events
    private void StartButton_Click(object sender, RoutedEventArgs e) {
      RiotServices.MatchmakerService.AttachToQueue(mmp);
    }

    private void InviteButton_Click(object sender, RoutedEventArgs e) {
      InvitePopup.BeginStoryboard(App.FadeIn);
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      ForceClose();
      Close?.Invoke(this, new EventArgs());
    }

    private void InvitePopup_Close(object sender, EventArgs e) {
      InvitePopup.BeginStoryboard(App.FadeOut);
      foreach (var user in InvitePopup.Users.Where(u => u.Value)) {
        double id;
        if (double.TryParse(user.Key.Replace("sum", ""), out id)) {
          RiotServices.GameInvitationService.Invite(id);
        } else Client.TryBreak("Cannot parse user " + user.Key);
      }
    }
    #endregion

    public Page Page => this;
    public bool CanClose => true;
    public bool CanPlay => false;
    public IQueuer HandleClose() => new ReturnToLobbyQueuer(this);

    public void ForceClose() {
      RiotServices.GameInvitationService.Leave();
      chatRoom.LeaveChat();
      Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
    }
  }
}
