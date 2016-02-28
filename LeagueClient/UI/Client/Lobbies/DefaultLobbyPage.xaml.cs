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
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;
using System.Timers;
using LeagueClient.UI.Client.Alerts;
using RiotClient.Riot.Platform;
using RiotClient.Lobbies;
using RiotClient;

namespace LeagueClient.UI.Client.Lobbies {
  /// <summary>
  /// Interaction logic for DefaultLobbyPage.xaml
  /// </summary>
  public sealed partial class DefaultLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private GameQueueConfig config;
    private ChatRoom chatRoom;
    private QueueLobby lobby;

    private QueueController queueTimer;
    private Queue queue;

    public DefaultLobbyPage(QueueLobby lobby) {
      InitializeComponent();

      this.lobby = lobby;

      lobby.MemberJoined += Lobby_MemberJoined;
      lobby.MemberLeft += Lobby_MemberLeft;

      lobby.QueueEntered += Lobby_QueueEntered;
      lobby.QueueLeft += Lobby_QueueLeft;

      lobby.LeftLobby += Lobby_LeftLobby;
      lobby.Loaded += Lobby_Loaded;

      lobby.CatchUp();

      config = Session.Current.AvailableQueues[lobby.QueueID];
      var map = GameMap.Maps.FirstOrDefault(m => config.SupportedMapIds.Contains(m.MapId));

      MapImage.Source = new BitmapImage(GameMap.Images[map]);
      MapLabel.Content = map.DisplayName;
      QueueLabel.Content = GameConfig.Values[config.GameTypeConfigId].Value;
      ModeLabel.Content = config.Ranked ? "Ranked" : ModeLabel.Content = GameMode.Values[config.GameMode].Value;
      TeamSizeLabel.Content = $"{config.NumPlayersPerTeam}v{config.NumPlayersPerTeam}";
    }

    private void Lobby_MemberJoined(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var member = e.Member as QueueLobbyMember;
        if (member != null) {
          var player = new LobbyPlayer2(lobby.IsCaptain, member, 0);
          PlayerList.Children.Add(player);

          var players = PlayerList.Children.Cast<LobbyPlayer2>().ToList();
          foreach (var control in players) {
            PlayerList.Children.Remove(control);
            int index = lobby.Members.IndexOf(control.Member);
            PlayerList.Children.Insert(index, control);
          }
        } else {
          var player = new InvitedPlayer(e.Member as LobbyInvitee);
          InviteList.Children.Add(player);
        }
      });
    }

    private void Lobby_MemberLeft(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var player = PlayerList.Children.Cast<LobbyPlayer2>().FirstOrDefault(p => p.Member == e.Member);
        PlayerList.Children.Remove(player);
      });
    }

    private void Lobby_QueueEntered(object sender, QueueEventArgs e) {
      queueTimer.Start();
      queue = e.Queue;
      Dispatcher.Invoke(() => {
        foreach (var control in PlayerList.Children)
          ((LobbyPlayer2) control).CanControl = false;

        QuitButton.Content = "Cancel";
        StartButton.Visibility = Visibility.Collapsed;
        QueueTimeLabel.Visibility = Visibility.Visible;
      });
    }

    private void Lobby_QueueLeft(object sender, EventArgs e) {
      queueTimer.Cancel();
      queue = null;
      Dispatcher.Invoke(() => {
        foreach (var control in PlayerList.Children)
          ((LobbyPlayer2) control).CanControl = lobby.IsCaptain;

        QuitButton.Content = "Quit";
        QueueTimeLabel.Visibility = Visibility.Collapsed;
        StartButton.Visibility = lobby.IsCaptain ? Visibility.Visible : Visibility.Hidden;
      });
    }

    private void Lobby_LeftLobby(object sender, EventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void Lobby_Loaded(object sender, EventArgs e) {
      queueTimer = new QueueController(QueueTimeLabel, ChatStatus.inQueue, lobby.IsCaptain ? ChatStatus.hostingNormalGame : ChatStatus.outOfGame);
      Session.Current.ChatManager.Status = lobby.IsCaptain ? ChatStatus.hostingNormalGame : ChatStatus.outOfGame;

      Dispatcher.Invoke(() => {
        chatRoom = new ChatRoom(lobby.ChatLobby, SendBox, ChatHistory, ChatSend, ChatScroller);
        LoadingGrid.Visibility = Visibility.Collapsed;
        StartButton.Visibility = lobby.IsCaptain ? Visibility.Visible : Visibility.Hidden;
      });
    }

    #region UI Events

    private void StartButton_Click(object sender, RoutedEventArgs e) {
      lobby.StartQueue();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      if (queue == null) {
        lobby.Dispose();
      } else {
        lobby.CancelQueue();
      }

    }

    #endregion

    Page IClientSubPage.Page => this;

    public void Dispose() {
      lobby.Dispose();
      queueTimer.Dispose();
      Session.Current.ChatManager.Status = ChatStatus.outOfGame;
    }
  }
}
