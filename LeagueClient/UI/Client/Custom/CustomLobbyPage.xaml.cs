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
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using MFroehlich.League.Assets;
using RtmpSharp.Messaging;
using LeagueClient.UI.Client.Lobbies;
using RiotClient.Riot.Platform;
using RiotClient.Lobbies;
using RiotClient;

namespace LeagueClient.UI.Client.Custom {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public sealed partial class CustomLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private CustomLobby lobby;
    private ChatRoom chatRoom;
    private bool hasStarted;

    public CustomLobbyPage(CustomLobby lobby) {
      InitializeComponent();

      this.lobby = lobby;

      lobby.MemberJoined += Lobby_MemberJoined;
      lobby.MemberChangedTeam += Lobby_MemberChangedTeam;
      lobby.MemberLeft += Lobby_MemberLeft;
      lobby.LeftLobby += Lobby_LeftLobby;
      lobby.Loaded += Lobby_Loaded;

      lobby.GameStarted += Lobby_GameStarted;
      lobby.Updated += Lobby_GotGameDTO;

      lobby.CatchUp();

      Session.Current.ChatManager.Status = ChatStatus.hostingPracticeGame;
    }

    private void Lobby_Loaded(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        LoadingGrid.Visibility = Visibility.Collapsed;
        chatRoom = new ChatRoom(lobby.ChatLobby, SendBox, ChatHistory, ChatSend, ChatScroller);
      });
    }

    private void Lobby_GotGameDTO(object sender, EventArgs args) {
      Dispatcher.Invoke(() => {
        StartButt.Visibility = lobby.IsCaptain ? Visibility.Visible : Visibility.Collapsed;

        var game = lobby.Data;
        var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.MapId);

        MapImage.Source = new BitmapImage(GameMap.Images[map]);
        MapLabel.Content = map.DisplayName;
        ModeLabel.Content = GameMode.Values[game.GameMode];
        QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
        TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";
      });
    }

    private void Lobby_MemberJoined(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var invitee = e.Member as LobbyInvitee;
        var member = e.Member as CustomLobbyMember;

        if (member != null) {
          StackPanel stack;

          if (member.Team == 0) stack = BlueTeam;
          else if (member.Team == 1) stack = RedTeam;
          else throw new Exception("UNEXPECTED TEAM");

          var player = new LobbyPlayer(member);
          stack.Children.Add(player);

          if (e.Member.IsMe) {
            RedJoin.Visibility = BlueJoin.Visibility = Visibility.Collapsed;
            if (member.Team != 0) BlueJoin.Visibility = Visibility.Visible;
            if (member.Team != 1) RedJoin.Visibility = Visibility.Visible;
          }

          Sort();
        } else {
          var player = new InvitedPlayer(invitee);
          InviteList.Children.Add(player);
        }
      });
    }

    private void Lobby_MemberChangedTeam(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var member = e.Member as CustomLobbyMember;
        StackPanel src, dst;

        if (member.Team == 1) {
          src = BlueTeam;
          dst = RedTeam;
        } else if (member.Team == 0) {
          src = RedTeam;
          dst = BlueTeam;
        } else throw new Exception("UNEXPECTED TEAM");

        var player = src.Children.Cast<LobbyPlayer>().FirstOrDefault(p => p.Member == member);
        src.Children.Remove(player);
        dst.Children.Add(player);

        if (e.Member.IsMe) {
          RedJoin.Visibility = BlueJoin.Visibility = Visibility.Collapsed;
          if (member.Team != 0) BlueJoin.Visibility = Visibility.Visible;
          if (member.Team != 1) RedJoin.Visibility = Visibility.Visible;
        }

        Sort();
      });
    }

    private void Lobby_MemberLeft(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var member = e.Member as CustomLobbyMember;
        StackPanel stack;

        if (member.Team == 0) stack = BlueTeam;
        else if (member.Team == 1) stack = RedTeam;
        else throw new Exception("UNEXPECTED TEAM");

        var player = stack.Children.Cast<LobbyPlayer>().FirstOrDefault(p => p.Member == e.Member);
        stack.Children.Remove(player);
      });
    }

    private void Lobby_LeftLobby(object sender, EventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void Lobby_GameStarted(object sender, GameLobby game) {
      hasStarted = true;
      LoLClient.MainWindow.BeginChampionSelect(game);
    }

    private void Sort() {
      var players = BlueTeam.Children.Cast<LobbyPlayer>().ToList();
      foreach (var player in players) {
        BlueTeam.Children.Remove(player);
        int index = lobby.TeamOne.IndexOf(player.Member);
        BlueTeam.Children.Insert(index, player);
      }

      players = RedTeam.Children.Cast<LobbyPlayer>().ToList();
      foreach (var player in players) {
        RedTeam.Children.Remove(player);
        int index = lobby.TeamTwo.IndexOf(player.Member);
        RedTeam.Children.Insert(index, player);
      }
    }

    #region UI Events

    private void RedJoin_Click(object sender, RoutedEventArgs e) {
      var me = lobby.AllMembers.SingleOrDefault(m => m.IsMe);
      if (me.Team == 2) {
        lobby.SwitchToPlayer(2);
      } else {
        lobby.SwitchTeams();
      }
    }

    private void BlueJoin_Click(object sender, RoutedEventArgs e) {
      var me = lobby.AllMembers.SingleOrDefault(m => m.IsMe);
      if (me.Team == 2) {
        lobby.SwitchToPlayer(1);
      } else {
        lobby.SwitchTeams();
      }
    }

    private void Spectate_Click(object sender, RoutedEventArgs e) {
      lobby.SwitchToObserver();
    }

    private void Start_Click(object sender, RoutedEventArgs e) {
      lobby.StartChampSelect();
    }

    private void Quit_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      Dispose();
    }

    #endregion

    public Page Page => this;
    public void Dispose() {
      if (!hasStarted)
        lobby.Dispose();

      lobby.MemberJoined -= Lobby_MemberJoined;
      lobby.MemberChangedTeam -= Lobby_MemberChangedTeam;
      lobby.MemberLeft -= Lobby_MemberLeft;
      lobby.LeftLobby -= Lobby_LeftLobby;
      lobby.Loaded -= Lobby_Loaded;

      lobby.GameStarted -= Lobby_GameStarted;
      lobby.Updated -= Lobby_GotGameDTO;

      Session.Current.ChatManager.Status = ChatStatus.outOfGame;
    }
  }
}
