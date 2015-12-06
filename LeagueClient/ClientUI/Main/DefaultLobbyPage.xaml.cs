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
  public partial class DefaultLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private MatchMakerParams mmp;
    private GameQueueConfig config;
    private ChatRoomController chatRoom;
    private LobbyStatus lobby;

    private Timer timer;
    private DateTime start;

    public DefaultLobbyPage() {
      InitializeComponent();

      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed;
    }

    public DefaultLobbyPage(MatchMakerParams mmp) : this() {
      this.mmp = mmp;
      chatRoom = new ChatRoomController(SendBox, ChatHistory, ChatSend, ChatScroller);
      Client.ChatManager.UpdateStatus(ChatStatus.hostingNormalGame);

      //InviteButton.Visibility = Visibility.Hidden;

      config = Client.AvailableQueues[mmp.QueueIds[0]];
      var map = GameMap.Maps.FirstOrDefault(m => config.SupportedMapIds.Contains(m.MapId));

      MapImage.Source = GameMap.Images[map];
      MapLabel.Content = map.DisplayName;
      QueueLabel.Content = GameConfig.Values[config.GameTypeConfigId].Value;
      ModeLabel.Content = config.Ranked ? "Ranked" : ModeLabel.Content = GameMode.Values[config.GameMode].Value;
      TeamSizeLabel.Content = $"{config.NumPlayersPerTeam}v{config.NumPlayersPerTeam}";
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Dispatcher.Invoke(() => QueueTimeLabel.Content = "In queue for " + elapsed.ToString("m\\:ss"));
    }

    #region RTMP Messages
    public bool HandleMessage(MessageReceivedEventArgs args) {
      var lobby = args.Body as LobbyStatus;
      var notify = args.Body as GameNotification;
      var invite = args.Body as InvitePrivileges;
      var queue = args.Body as SearchingForMatchNotification;
      var game = args.Body as GameDTO;

      if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        Client.CanInviteFriends = invite.canInvite;
        //Dispatcher.Invoke(() => InviteButton.Visibility = invite.canInvite ? Visibility.Visible : Visibility.Collapsed);
        return true;
      } else if (queue != null) {
        EnterQueue(queue);
        return true;
      } else if (notify != null) {
        SetInQueue(false);
        return true;
      } else if (game != null) {
        Dispatcher.Invoke(() => Client.QueueManager.ShowQueuePopup(new DefaultQueuePopup(game)));
        return true;
      }

      return false;
    }

    public void GotLobbyStatus(LobbyStatus lobby) {
      this.lobby = lobby;

      mmp.InvitationId = lobby.InvitationID;
      mmp.Team = lobby.Members.Select(m => (int) m.SummonerId).ToList();

      if (!chatRoom.IsJoined)
        chatRoom.JoinChat(RiotChat.GetLobbyRoom(lobby.InvitationID, lobby.ChatKey), lobby.ChatKey);

      Dispatcher.Invoke(() => {
        InviteList.Children.Clear();
        foreach (var player in lobby.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
          InviteList.Children.Add(new InvitedPlayer(player));
        }
        PlayerList.Children.Clear();
        foreach (var player in lobby.Members) {
          var control = new LobbyPlayer2(lobby.Owner.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId, player);
          control.GiveInviteClicked += GiveInviteClicked;
          control.KickClicked += KickClicked;
          PlayerList.Children.Add(control);
        }

        bool owner = lobby.Owner.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId;
        StartButton.Visibility = owner ? Visibility.Visible : Visibility.Hidden;
        Client.CanInviteFriends = owner;
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
      if (timer.Enabled) {
        RiotServices.MatchmakerService.PurgeFromQueues();
        SetInQueue(false);
      } else {
        ForceClose();
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
        Client.ChatManager.UpdateStatus(ChatStatus.inQueue);
        start = DateTime.Now;
        timer.Start();
        Dispatcher.Invoke(() => {
          QuitButton.Content = "Cancel";
          StartButton.Visibility = Visibility.Collapsed;
          QueueTimeLabel.Visibility = Visibility.Visible;
          QueueTimeLabel.Content = "In queue for 00:00";
        });
      } else {
        Client.ChatManager.UpdateStatus(ChatStatus.hostingNormalGame);
        timer.Stop();
        Dispatcher.Invoke(() => {
          QuitButton.Content = "Quit";
          QueueTimeLabel.Visibility = Visibility.Collapsed;
          bool owner = lobby.Owner.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId;
          StartButton.Visibility = owner ? Visibility.Visible : Visibility.Hidden;
        });
      }
    }

    private void EnterQueue(SearchingForMatchNotification search) {
      if (Client.QueueManager.AttachToQueue(search))
        SetInQueue(true);
    }

    public Page Page => this;

    public void ForceClose() {
      RiotServices.GameInvitationService.Leave();
      chatRoom.LeaveChat();
      Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
    }
  }
}
