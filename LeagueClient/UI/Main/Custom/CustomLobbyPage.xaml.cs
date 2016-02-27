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
using LeagueClient.UI.Main.Lobbies;
using RiotClient.Riot.Platform;
using RiotClient.Lobbies;
using RiotClient;

namespace LeagueClient.UI.Main.Custom {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public sealed partial class CustomLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private CustomGame lobby;
    private ChatRoom chatRoom;
    private bool hasStarted;

    public CustomLobbyPage(CustomGame lobby) {
      InitializeComponent();

      this.lobby = lobby;

      lobby.MemberJoined += Lobby_MemberJoined;
      lobby.MemberChangedTeam += Lobby_MemberChangedTeam;
      lobby.MemberLeft += Lobby_MemberLeft;
      lobby.Lobby.LeftLobby += Lobby_LeftLobby;
      lobby.Lobby.Loaded += Lobby_Loaded;
      lobby.Lobby.PlayerInvited += Lobby_PlayerInvited;

      lobby.EnteredChampSelect += Lobby_EnteredChampSelect;
      lobby.Updated += Lobby_GotGameDTO;

      lobby.CatchUp();

      chatRoom = new ChatRoom(lobby.Lobby, SendBox, ChatHistory, ChatSend, ChatScroller);
      Session.Current.ChatManager.Status = ChatStatus.hostingPracticeGame;
    }

    private void Lobby_Loaded(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        StartButt.Visibility = lobby.Lobby.IsCaptain ? Visibility.Visible : Visibility.Collapsed;
        LoadingGrid.Visibility = Visibility.Collapsed;
      });
    }

    private void Lobby_GotGameDTO(object sender, EventArgs args) {
      Dispatcher.Invoke(() => {
        var game = lobby.Data;
        var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.MapId);

        MapImage.Source = new BitmapImage(GameMap.Images[map]);
        MapLabel.Content = map.DisplayName;
        ModeLabel.Content = GameMode.Values[game.GameMode];
        QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
        TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";
      });
    }

    private void Lobby_MemberJoined(object sender, GameMemberEventArgs e) {
      Dispatcher.Invoke(() => {
        StackPanel stack;

        if (e.Member.Team == 0) stack = BlueTeam;
        else if (e.Member.Team == 1) stack = RedTeam;
        else throw new Exception("UNEXPECTED TEAM");

        var player = new LobbyPlayer(e.Member);
        stack.Children.Add(player);

        if (e.Member.IsMe) {
          RedJoin.Visibility = BlueJoin.Visibility = Visibility.Collapsed;
          if (e.Member.Team != 0) BlueJoin.Visibility = Visibility.Visible;
          if (e.Member.Team != 1) RedJoin.Visibility = Visibility.Visible;
        }

        Sort();
      });
    }

    private void Lobby_MemberChangedTeam(object sender, GameMemberEventArgs e) {
      Dispatcher.Invoke(() => {
        StackPanel stack;
        StackPanel other;

        if (e.Member.Team == 1) {
          stack = BlueTeam; other = RedTeam;
        } else if (e.Member.Team == 0) {
          stack = RedTeam; other = BlueTeam;
        } else throw new Exception("UNEXPECTED TEAM");

        var player = stack.Children.Cast<LobbyPlayer>().FirstOrDefault(p => p.Member == e.Member);
        stack.Children.Remove(player);
        other.Children.Add(player);

        if (e.Member.IsMe) {
          RedJoin.Visibility = BlueJoin.Visibility = Visibility.Collapsed;
          if (e.Member.Team != 0) BlueJoin.Visibility = Visibility.Visible;
          if (e.Member.Team != 1) RedJoin.Visibility = Visibility.Visible;
        }

        Sort();
      });
    }

    private void Lobby_MemberLeft(object sender, GameMemberEventArgs e) {
      Dispatcher.Invoke(() => {
        StackPanel stack;

        if (e.Member.Team == 0) stack = BlueTeam;
        else if (e.Member.Team == 1) stack = RedTeam;
        else throw new Exception("UNEXPECTED TEAM");

        var player = stack.Children.Cast<LobbyPlayer>().FirstOrDefault(p => p.Member == e.Member);
        stack.Children.Remove(player);
      });
    }

    private void Lobby_LeftLobby(object sender, EventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void Lobby_PlayerInvited(object sender, InviteeEventArgs e) {
      Dispatcher.Invoke(() => {
        var player = new InvitedPlayer(e.Invitee);
        InviteList.Children.Add(player);
      });
    }

    private void Lobby_EnteredChampSelect(object sender, EventArgs game) {
      hasStarted = true;
      Client.MainWindow.BeginChampionSelect(lobby);
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
        lobby.Quit();

      lobby.MemberJoined -= Lobby_MemberJoined;
      lobby.MemberChangedTeam -= Lobby_MemberChangedTeam;
      lobby.MemberLeft -= Lobby_MemberLeft;
      lobby.Lobby.LeftLobby -= Lobby_LeftLobby;
      lobby.Lobby.Loaded -= Lobby_Loaded;
      lobby.Lobby.PlayerInvited -= Lobby_PlayerInvited;

      lobby.EnteredChampSelect -= Lobby_EnteredChampSelect;
      lobby.Updated -= Lobby_GotGameDTO;

      Session.Current.ChatManager.Status = ChatStatus.outOfGame;
    }
  }
}
