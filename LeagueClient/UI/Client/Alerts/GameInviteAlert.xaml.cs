using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MFroehlich.Parsing.JSON;
using RiotClient.Riot.Platform;
using RiotClient;

namespace LeagueClient.UI.Client.Alerts {
  /// <summary>
  /// Interaction logic for YesNoAlert.xaml
  /// </summary>
  public partial class GameInviteAlert : UserControl, Alert, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<bool> Close;

    public string Message {
      get { return message; }
      set { SetField(ref message, value); }
    }
    public string MapName {
      get { return mapName; }
      set { SetField(ref mapName, value); }
    }
    public string ModeName {
      get { return modeName; }
      set { SetField(ref modeName, value); }
    }
    public string QueueName {
      get { return queueName; }
      set { SetField(ref queueName, value); }
    }
    public string TypeName {
      get { return typeName; }
      set { SetField(ref typeName, value); }
    }

    private string message;
    private string mapName;
    private string modeName;
    private string queueName;
    private string typeName;

    private InvitationRequest invite;
    private JSONObject metaData;

    public GameInviteAlert() {
      InitializeComponent();
    }

    public GameInviteAlert(InvitationRequest invite) : this() {
      this.invite = invite;
      this.metaData = JSONParser.ParseObject(invite.GameMetaData, 0);
      var map = GameMap.Maps.FirstOrDefault(m => m.MapId == (int) metaData["mapId"]);

      Message = invite.Inviter.summonerName + " has invited you to play a game";
      MapName = map.DisplayName;
      ModeName = GameMode.Values[(string) metaData["gameMode"]].Value;
      QueueName = GameConfig.Values[(int) metaData["gameTypeConfigId"]].Value;
      if ((int) metaData["gameTypeConfigId"] == 12) {
        TypeName = "Team Builder";
      } else {
        switch ((string) metaData["gameType"]) {
          case "PRACTICE_GAME": TypeName = "Custom"; break;
          case "NORMAL_GAME": TypeName = "Normal"; break;
          case "RANKED_TEAM_GAME": TypeName = "Ranked"; break;
          default: break;
        }
      }

      HistoryGrid.DataContext = this;
      PopupGrid.DataContext = this;
      MainGrid.Children.Remove(HistoryGrid);
      MainGrid.Children.Remove(PopupGrid);
    }

    public UIElement Popup => PopupGrid;
    public UIElement History => HistoryGrid;

    private void Accept_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, true);

      //Client.QueueManager.AcceptInvite(invite);


      //var task = RiotServices.GameInvitationService.Accept(invite.InvitationId);
      //if ((int) metaData["gameTypeConfigId"] == 12) {
      //  var lobby = new CapLobbyPage(false);
      //  task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      //  RiotServices.CapService.JoinGroupAsInvitee((string) metaData["groupFinderId"]);
      //  Session.Current.QueueManager.ShowPage(lobby);
      //} else {
      //  switch ((string) metaData["gameType"]) {
      //    case "PRACTICE_GAME":
      //      var custom = new CustomLobbyPage();
      //      task.ContinueWith(t => custom.GotLobbyStatus(t.Result));
      //      Session.Current.QueueManager.ShowPage(custom);
      //      break;
      //    case "NORMAL_GAME":
      //      var normal = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] } });
      //      task.ContinueWith(t => normal.GotLobbyStatus(t.Result));
      //      Session.Current.QueueManager.ShowPage(normal);
      //      break;
      //    case "RANKED_TEAM_GAME":
      //      var ranked = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] }, TeamId = new TeamId { FullId = (string) metaData["rankedTeamId"] } });
      //      task.ContinueWith(t => ranked.GotLobbyStatus(t.Result));
      //      Session.Current.QueueManager.ShowPage(ranked);
      //      break;
      //  }
      //}
    }

    private void Decline_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, true);

      //TODO RiotServices.GameInvitationService.Decline(invite.InvitationId);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close?.Invoke(this, false);
    private void CloseAgain_Click(object sender, RoutedEventArgs e) => Close?.Invoke(this, true);

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }
  }
}
