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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using RtmpSharp.Messaging;
using LeagueClient.Logic.Cap;
using System.Timers;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.JSON;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderSoloPage.xaml
  /// </summary>
  public sealed partial class CapSoloPage : Page, IClientSubPage, IDisposable {
    private CapMePlayer me;
    private QueueController queue;

    public event EventHandler Close;

    public CapSoloPage(CapPlayer me = null) {
      InitializeComponent();
      Client.PopupSelector.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                                   where spell.modes.Contains("CLASSIC")
                                                   select spell);

      this.me = new CapMePlayer(me, CapMePlayer.CapControlState.Complete);
      this.me.CapPlayer.CapEvent += PlayerUpdate;
      if (me != null) this.me.CapPlayer = me;

      MeArea.Child = this.me;

      Client.ChatManager.Status = ChatStatus.inTeamBuilder;

      queue = new QueueController(QueueInfoLabel, ChatStatus.inTeamBuilder, ChatStatus.inTeamBuilder);
      if (me != null) SetInQueue(true);
    }

    private void SetInQueue(bool inQueue) {
      if (inQueue) {
        queue.Start();
      } else {
        queue.Cancel();
      }

      Dispatcher.Invoke(() => PlayerUpdate(null, null));
    }

    private void PlayerUpdate(object sender, EventArgs e) {
      GameMap.UpdateList(new[] { me.CapPlayer });

      me.Editable = queue.InQueue ? CapMePlayer.CapControlState.None : CapMePlayer.CapControlState.Complete;
      if (queue.InQueue) {
        EnterQueueButt.BeginStoryboard(App.FadeOut);
        QueueInfoLabel.Visibility = Visibility.Visible;
        QuitButt.Content = "Cancel";
      } else {
        QuitButt.Content = "Quit";
        QueueInfoLabel.Visibility = Visibility.Collapsed;
        if (me.CapPlayer.CanBeReady()) {
          EnterQueueButt.BeginStoryboard(App.FadeIn);
        }
      }
    }

    private void EnterQueue(object sender, RoutedEventArgs e) {
      var id = RiotServices.CapService.CreateSoloQuery(me.CapPlayer);
      RiotServices.AddHandler(id, response => {
        if (response.status.Equals("OK")) {
          SetInQueue(true);
        }
      });
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      if (queue.InQueue) {
        RiotServices.CapService.Quit();
        SetInQueue(false);
      } else {
        ForceClose();
        Close?.Invoke(this, new EventArgs());
      }
    }

    public bool HandleMessage(MessageReceivedEventArgs e) {
      var response = e.Body as LcdsServiceProxyResponse;
      if (response != null) {
        switch (response.methodName) {
          case "acceptedByGroupV2":
            queue.Dispose();
            Dispatcher.Invoke(() => {
              var popup = new CapSoloQueuePopup(JSONParser.ParseObject(response.payload, 0), me.CapPlayer);
              popup.Close += (src, e2) => SetInQueue(false);
              Client.QueueManager.ShowQueuePopup(popup);
            });
            return true;
        }
      }
      return false;
    }

    public Page Page => this;
    public void ForceClose() => Client.ChatManager.Status = ChatStatus.outOfGame;

    public void Dispose() {
      queue.Dispose();
      me.Dispose();
    }
  }
}
