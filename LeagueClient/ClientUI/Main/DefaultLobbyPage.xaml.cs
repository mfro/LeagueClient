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
using System.Timers;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for DefaultLobbyPage.xaml
  /// </summary>
  public sealed partial class DefaultLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private MatchMakerParams mmp;
    private GameQueueConfig config;
    private ChatRoom chatRoom;
    private LobbyStatus lobby;

    private QueueController queue;

    public DefaultLobbyPage() {
      InitializeComponent();
    }

    public DefaultLobbyPage(MatchMakerParams mmp) : this() {
      this.mmp = mmp;
      chatRoom = new ChatRoom(SendBox, ChatHistory, ChatSend, ChatScroller);

      //InviteButton.Visibility = Visibility.Hidden;

      config = Client.Session.AvailableQueues[mmp.QueueIds[0]];
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
      var notify = args.Body as GameNotification;
      var invite = args.Body as InvitePrivileges;
      var queue = args.Body as SearchingForMatchNotification;
      var msg = args.Body as SimpleDialogMessage;
      var game = args.Body as GameDTO;

      if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        Client.Session.CanInviteFriends = invite.canInvite;
        return true;
      } else if (queue != null) {
        EnterQueue(queue);
        return true;
      } else if (notify != null) {
        SetInQueue(false);
        return true;
      } else if (msg != null) {
        if (msg.titleCode.Equals("ready_check.penalty.applied")) {
          //TODO Failed to accept notifcation
          return true;
        }
      } else if (game != null) {
        Dispatcher.Invoke(() => {
          var popup = new DefaultQueuePopup(game);
          popup.Close += (src, e) => SetInQueue(false);
          Client.Session.QueueManager.ShowQueuePopup(popup);
        });
        return true;
      }

      return false;
    }

    public void GotLobbyStatus(LobbyStatus lobby) {
      this.lobby = lobby;

      bool owner = lobby.Owner.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId;
      Client.Session.ChatManager.Status = owner ? ChatStatus.hostingNormalGame : ChatStatus.outOfGame;

      mmp.InvitationId = lobby.InvitationID;
      mmp.Team = lobby.Members.Select(m => (int) m.SummonerId).ToList();

      if (!chatRoom.IsJoined) {
        chatRoom.JoinChat(RiotChat.GetLobbyRoom(lobby.InvitationID, lobby.ChatKey), lobby.ChatKey);
        queue = new QueueController(QueueTimeLabel, ChatStatus.inQueue, owner ? ChatStatus.hostingNormalGame : ChatStatus.outOfGame);
      }

      Dispatcher.Invoke(() => {
        LoadingGrid.Visibility = Visibility.Collapsed;
        InviteList.Children.Clear();
        foreach (var player in lobby.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
          InviteList.Children.Add(new InvitedPlayer(player));
        }
        PlayerList.Children.Clear();
        foreach (var player in lobby.Members) {
          var user = RiotChat.GetUser(player.SummonerId);
          int icon = Client.Session.Settings.ProfileIcon;
          if (!owner && chatRoom.Statuses.ContainsKey(user)) icon = chatRoom.Statuses[RiotChat.GetUser(player.SummonerId)].ProfileIcon;

          var control = new LobbyPlayer2(owner, player, icon);
          control.GiveInviteClicked += GiveInviteClicked;
          control.KickClicked += KickClicked;
          PlayerList.Children.Add(control);
        }

        StartButton.Visibility = owner ? Visibility.Visible : Visibility.Hidden;
        Client.Session.CanInviteFriends = owner;
      });
    }
    #endregion

    #region UI Events
    private void GiveInviteClicked(object sender, EventArgs e) {
      var member = lobby.Members.FirstOrDefault(m => m.SummonerId == ((LobbyPlayer2) sender).SummonerId);
      if (member.HasInvitePower)
        RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerId);
      else
        RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerId);
      member.HasInvitePower = !member.HasInvitePower;
    }

    private void KickClicked(object sender, EventArgs e) {
      throw new NotImplementedException();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e) {
      var search = await RiotServices.MatchmakerService.AttachTeamToQueue(mmp);
      EnterQueue(search);
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      if (queue.InQueue) {
        RiotServices.MatchmakerService.PurgeFromQueues();
        SetInQueue(false);
      } else {
        Dispose();
        Close?.Invoke(this, new EventArgs());
      }
    }
    #endregion

    private void SetInQueue(bool inQueue) {
      Dispatcher.Invoke(() => {
        foreach (var control in PlayerList.Children)
          ((LobbyPlayer2) control).CanControl = !inQueue;
      });

      if (inQueue) {
        queue.Start();
        Dispatcher.Invoke(() => {
          QuitButton.Content = "Cancel";
          StartButton.Visibility = Visibility.Collapsed;
          QueueTimeLabel.Visibility = Visibility.Visible;
        });
      } else {
        queue.Cancel();
        Dispatcher.Invoke(() => {
          QuitButton.Content = "Quit";
          QueueTimeLabel.Visibility = Visibility.Collapsed;
          bool owner = lobby.Owner.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId;
          StartButton.Visibility = owner ? Visibility.Visible : Visibility.Hidden;
        });
      }
    }

    private void EnterQueue(SearchingForMatchNotification search) {
      if (Client.Session.QueueManager.AttachToQueue(search))
        SetInQueue(true);
    }

    public Page Page => this;

    public void Dispose() {
      RiotServices.GameInvitationService.Leave();
      chatRoom.Dispose();
      queue.Dispose();
      Client.Session.ChatManager.Status = ChatStatus.outOfGame;
    }
  }
}
