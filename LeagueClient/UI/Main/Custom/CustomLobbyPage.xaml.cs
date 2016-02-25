﻿using System;
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
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using RtmpSharp.Messaging;
using LeagueClient.UI.Main.Lobbies;

namespace LeagueClient.UI.Main.Custom {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public sealed partial class CustomLobbyPage : Page, IClientSubPage {
    public GameDTO GameDto { get; private set; }
    public bool IsCaptain => lobby?.Owner.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId;

    private LobbyStatus lobby;
    private ChatRoom chatRoom;

    #region Constructors

    public CustomLobbyPage(GameDTO game) : this() {
      GotGameData(game);
      Client.Session.ChatManager.Status = ChatStatus.teamSelect;
    }

    public CustomLobbyPage() {
      InitializeComponent();
      chatRoom = new ChatRoom(SendBox, ChatHistory, ChatSend, ChatScroller);
      Client.Session.ChatManager.Status = ChatStatus.hostingPracticeGame;
    }

    #endregion

    public bool HandleMessage(MessageReceivedEventArgs e) {
      GameDTO game;
      LobbyStatus status;
      InvitePrivileges invite;
      if ((game = e.Body as GameDTO) != null) {
        Dispatcher.MyInvoke(GotGameData, game);
        return true;
      } else if ((status = e.Body as LobbyStatus) != null) {
        GotLobbyStatus(status);
        return true;
      } else if ((invite = e.Body as InvitePrivileges) != null) {
        Client.Session.CanInviteFriends = invite.canInvite;
        //Dispatcher.Invoke(() => InviteButt.Visibility = invite.canInvite ? Visibility.Visible : Visibility.Collapsed);
      }
      return false;
    }

    public void GotLobbyStatus(LobbyStatus status) {
      lobby = status;
      Dispatcher.Invoke(() => {
        if (IsCaptain) {
          Client.Session.CanInviteFriends = true;
          StartButt.Visibility = Visibility.Visible;
        } else {
          StartButt.Visibility = Visibility.Collapsed;
        }

        InviteList.Children.Clear();
        foreach (var player in status.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
          InviteList.Children.Add(new InvitedPlayer(player));
        }
      });
    }

    public void GotGameData(GameDTO game) {
      GameDto = game;
      if (!chatRoom.IsJoined) {
        chatRoom.JoinChat(RiotChat.GetCustomRoom(game.Name, game.Id, game.RoomPassword), game.RoomPassword);

        StartButt.Visibility = (game.OwnerSummary.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId) ? Visibility.Visible : Visibility.Collapsed;

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

          //if (game.OwnerSummary.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SumId)
          //  InviteButt.Visibility = Visibility.Visible;

          foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
            bool blue = game.TeamOne.Contains(thing);
            var player = thing as PlayerParticipant;
            var bot = thing as BotParticipant;

            UserControl control;
            if (player != null) control = new LobbyPlayer(player, true);
            else if (bot != null) control = new LobbyPlayer(bot, true);
            else throw new NotImplementedException(thing.GetType().Name);

            Dispatcher.BeginInvoke((Action) (() => {
              BlueTeam.Height = RedTeam.Height = control.ActualHeight * (game.MaxNumPlayers / 2);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (blue) {
              BlueTeam.Children.Add(control);
              if (player?.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId) {
                RedJoin.Visibility = Visibility.Visible;
                BlueJoin.Visibility = Visibility.Collapsed;
              }
            } else {
              RedTeam.Children.Add(control);
              if (player?.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId) {
                RedJoin.Visibility = Visibility.Collapsed;
                BlueJoin.Visibility = Visibility.Visible;
              }
            }
          }

          if (game.TeamOne.Count == game.MaxNumPlayers / 2)
            BlueJoin.Visibility = Visibility.Collapsed;
          if (game.TeamTwo.Count == game.MaxNumPlayers / 2)
            RedJoin.Visibility = Visibility.Collapsed;

          foreach (var thing in game.Observers) {
            ObserverList.Children.Add(new Label { Content = thing.SummonerName });
          }
        });
      } else if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        chatRoom.Dispose();
        Close?.Invoke(this, new EventArgs());
        Client.MainWindow.BeginChampionSelect(game);
      }
    }

    #region IClientSubPage

    public event EventHandler Close;

    public Page Page => this;

    public void Dispose() {
      chatRoom.Dispose();
      RiotServices.GameService.QuitGame();
      Client.Session.ChatManager.Status = ChatStatus.outOfGame;
    }

    #endregion

    #region UI Events

    private void Quit_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      Dispose();
    }

    private void RedJoin_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.SwitchTeams(GameDto.Id);
    }

    private void BlueJoin_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.SwitchTeams(GameDto.Id);
    }

    private void Spectate_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.SwitchPlayerToObserver(GameDto.Id);
    }

    private void Start_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.StartChampionSelection(GameDto.Id, GameDto.OptimisticLock);
    }

    #endregion
  }
}
