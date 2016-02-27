﻿using System;
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
using RtmpSharp.Messaging;
using MFroehlich.Parsing.JSON;
using LeagueClient.Logic;
using System.Reflection;
using LeagueClient.Logic.Chat;
using RiotClient.Lobbies;

namespace LeagueClient.UI.Main.Lobbies {
  /// <summary>
  /// Interaction logic for TBDLobbyPage.xaml
  /// </summary>
  public partial class TBDLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private TBDLobby lobby;
    private ChatRoom chat;

    public TBDLobbyPage(TBDLobby lobby) {
      InitializeComponent();
      this.lobby = lobby;
      LoadingGrid.Visibility = Visibility.Visible;

      chat = new ChatRoom(lobby, SendBox, ChatHistory, SendButt, ChatScroller);
      lobby.Loaded += Lobby_Loaded;
      lobby.MemberJoined += Lobby_MemberJoined;
      lobby.OnRemovedFromService += Lobby_RemovedFromService;

      lobby.CatchUp();
    }

    #region | Lobby Events |

    private void Lobby_Loaded(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        LoadingGrid.Visibility = Visibility.Collapsed;

        var spots = new[] { Pos0, Pos1, Pos2, Pos3, Pos4 };
        foreach (var spot in spots) spot.Child = null;
      });
    }

    private void Lobby_MemberJoined(object sender, MemberEventArgs e) {
      var spots = new[] { Pos0, Pos1, Pos2, Pos3, Pos4 };

      Dispatcher.Invoke(() => {
        var member = e.Member as TBDLobby.TBDLobbyMember;
        var player = new TBDPlayer(lobby.IsCaptain, member, 0);

        spots[member.SlotID].Child = player;
      });
    }

    #endregion

    #region | LCDS Events |

    private void Lobby_RemovedFromService(object sender, TBDLobby.RemovedFromServiceEventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    #endregion

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      lobby.Quit();
    }

    private void TBDPlayer_RoleSelected(object sender, RoleChangedEventArgs e) {
      lobby.SetRole(e.RoleIndex, e.Role);
    }

    public Page Page => this;

    public void Dispose() {
      lobby.Quit();
    }
  }
}