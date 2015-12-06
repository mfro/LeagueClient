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
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderSoloPage.xaml
  /// </summary>
  public sealed partial class CapSoloPage : Page, IClientSubPage, IDisposable {
    private bool spell1;
    private CapMePlayer me;
    private Timer timer;
    private DateTime start;

    public event EventHandler Close;

    public CapSoloPage(CapPlayer me = null) {
      InitializeComponent();
      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains("CLASSIC")
                                    select spell);

      this.me = new CapMePlayer(me);
      this.me.Editable = true;
      this.me.PlayerUpdate += PlayerUpdate;
      this.me.ChampClicked += Champion_Click;
      this.me.Spell1Clicked += Spell1_Click;
      this.me.Spell2Clicked += Spell2_Click;
      this.me.MasteryClicked += Player_MasteryClicked;
      this.me.RuneClicked += Player_RuneClicked;

      MeArea.Child = this.me;

      Popup.SpellSelector.SpellSelected += Spell_Select;
      Popup.ChampSelector.SkinSelected += ChampSelector_SkinSelected;

      Client.ChatManager.UpdateStatus(ChatStatus.inTeamBuilder);

      timer = new Timer(1000);
      timer.Elapsed += Time_Elapsed; ;
      if (me != null) SetInQueue(true);
    }

    private void SetInQueue(bool inQueue) {
      if (inQueue) {
        start = DateTime.Now;
        timer.Start();
      } else {
        timer.Stop();
      }

      Time_Elapsed(timer, null);
      Dispatcher.Invoke(() => PlayerUpdate(null, null));
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Dispatcher.Invoke(() => QueueInfoLabel.Content = "In queue for " + elapsed.ToString("m\\:ss"));
    }

    private void PlayerUpdate(object sender, EventArgs e) {
      GameMap.UpdateList(new[] { me.CapPlayer });

      me.Editable = !timer.Enabled;
      if (timer.Enabled) {
        EnterQueueButt.BeginStoryboard(App.FadeOut);
        QueueInfoLabel.Visibility = Visibility.Visible;
        QuitButt.Content = "Cancel";
      } else {
        QuitButt.Content = "Quit";
        QueueInfoLabel.Visibility = Visibility.Collapsed;
        if (me.CanBeReady()) {
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
      if (timer.Enabled) {
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
            timer.Dispose();
            Dispatcher.Invoke(() => Client.QueueManager.ShowQueuePopup(new CapSoloQueuePopup(JSON.ParseObject(response.payload), me.CapPlayer)));
            return true;
        }
      }
      return false;
    }

    #region Player Editing
    private void Player_MasteryClicked(object src, EventArgs args) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
    }

    private void Player_RuneClicked(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Runes;
    }

    private void Spell1_Click(object src, EventArgs args) {
      spell1 = true;
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
    }

    private void Spell2_Click(object src, EventArgs args) {
      spell1 = false;
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
    }

    private void Champion_Click(object src, EventArgs args) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Champions;
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
      Popup.MasteryEditor.Save().Wait();
      me.UpdateBooks();
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      me.CapPlayer.Champion = Popup.ChampSelector.SelectedChampion;
      me.Skin = e;
      Popup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) me.CapPlayer.Spell1 = spell;
      else me.CapPlayer.Spell2 = spell;
      Popup.BeginStoryboard(App.FadeOut);
    }
    #endregion

    public Page Page => this;
    public void ForceClose() => Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);

    public void Dispose() {
      timer.Dispose();
    }
  }
}
