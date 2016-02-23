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
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;
using LeagueClient.Logic.com.riotgames.other;
using MFroehlich.Parsing.JSON;
using LeagueClient.Logic;
using System.Reflection;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Chat;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TBDLobbyPage.xaml
  /// </summary>
  public partial class TBDLobbyPage : Page, IClientSubPage {
    public event EventHandler Close;

    private LobbyStatus lobby;
    private TBDGroupData data;
    private ChatRoom chat;

    public TBDLobbyPage() {
      InitializeComponent();

      chat = new ChatRoom(SendBox, ChatHistory, SendButt, ChatScroller);
    }

    private void JoinChat() {

    }

    public void GotLobbyStatus(LobbyStatus obj) {
      lobby = obj;
      if (data != null) JoinChat();
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      var lcds = args.Body as LcdsServiceProxyResponse;

      if (lcds != null && lcds.serviceName == RiotServices.TeambuilderDraftService.ServiceName) {
        if (lcds.status.Equals("OK")) {
          JSONObject json = null;
          if (!string.IsNullOrEmpty(lcds.payload)) json = JSONParser.ParseObject(lcds.payload, 0);
          var method = GetMethod(lcds);
          if (method != null)
            method(json);
          return true;
        } else if (!lcds.status.Equals("ACK")) Client.Log(lcds.status + ": " + lcds.payload);
      }
      return false;
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e) {
      RiotServices.TeambuilderDraftService.QuitV2();
    }

    #region LCDS

    private static readonly Dictionary<string, string> MethodNames = new Dictionary<string, string> {

    };

    private Action<JSONObject> GetMethod(LcdsServiceProxyResponse response) {
      JSONObject json = null;
      if (response.payload != null) {
        json = JSONParser.ParseObject(response.payload, 0);
      }
      Client.Log(response.methodName + ": " + response.payload);
      var name = response.methodName;
      if (MethodNames.ContainsKey(name)) name = MethodNames[name];
      var method = GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
      if (method != null)
        return (Action<JSONObject>) method.CreateDelegate(typeof(Action<JSONObject>), this);
      else return null;
    }

    private void tbdGameDtoV1(JSONObject arg) {
      data = JSONDeserializer.Deserialize<TBDGroupData>(arg);
      if (lobby != null) JoinChat();
    }

    private void removedFromServiceV1(JSONObject arg) {
      Close?.Invoke(this, new EventArgs());
    }

    #endregion

    public Page Page => this;

    public void Dispose() {
      RiotServices.GameInvitationService.Leave();
    }
  }
}
