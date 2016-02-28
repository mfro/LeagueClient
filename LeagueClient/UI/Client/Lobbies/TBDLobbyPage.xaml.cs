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
using RtmpSharp.Messaging;
using MFroehlich.Parsing.JSON;
using LeagueClient.Logic;
using System.Reflection;
using LeagueClient.Logic.Chat;
using RiotClient.Lobbies;
using RiotClient;

namespace LeagueClient.UI.Client.Lobbies {
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

      lobby.Loaded += Lobby_Loaded;
      lobby.MemberJoined += Lobby_MemberJoined;
      //lobby.OnRemovedFromService += Lobby_RemovedFromService;

      lobby.CatchUp();
    }

    #region | Lobby Events |

    private void Lobby_Loaded(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        LoadingGrid.Visibility = Visibility.Collapsed;
        chat = new ChatRoom(lobby.ChatLobby, SendBox, ChatHistory, SendButt, ChatScroller);
      });
    }

    private void Lobby_MemberJoined(object sender, MemberEventArgs e) {
      Dispatcher.Invoke(() => {
        var member = e.Member as TBDLobbyMember;
        if (member != null) {
          var spots = new[] { Pos0, Pos1, Pos2, Pos3, Pos4 };

          var player = new TBDPlayer(lobby.IsCaptain, member, 0);

          spots[member.SlotID].Child = player;
        } else {
          Session.Log("Invite: " + e.Member.Name);
        }
      });
    }

    #endregion

    #region | LCDS Events |

    //private void Lobby_RemovedFromService(object sender, TBDLobby.RemovedFromServiceEventArgs e) {
    //  Close?.Invoke(this, new EventArgs());
    //}

    #endregion

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      lobby.Dispose();
    }

    private void TBDPlayer_RoleSelected(object sender, RoleChangedEventArgs e) {
      //lobby.SetRole(e.RoleIndex, e.Role);
    }

    public Page Page => this;

    public void Dispose() {
      lobby.Dispose();
    }
  }
}
