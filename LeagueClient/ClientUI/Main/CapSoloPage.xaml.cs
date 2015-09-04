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

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderSoloPage.xaml
  /// </summary>
  public partial class CapSoloPage : Page, IClientSubPage {
    private bool spell1;

    public event EventHandler Close;

    public CapSoloPage() {
      InitializeComponent();
      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains("CLASSIC")
                                    select spell);

      Player.Editable = true;
      Player.PlayerUpdate += PlayerUpdate;
      Player.ChampClicked += Champion_Click;
      Player.Spell1Clicked += Spell1_Click;
      Player.Spell2Clicked += Spell2_Click;
      Player.MasteryClicked += Player_MasteryClicked;

      Popup.SpellSelector.SpellSelected += Spell_Select;
      Popup.ChampSelector.SkinSelected += ChampSelector_SkinSelected;

      Client.ChatManager.UpdateStatus(ChatStatus.inTeamBuilder);
    }

    private void PlayerUpdate(object sender, EventArgs e) {
      GameMap.UpdateList(new[] { Player.CapPlayer });
      if (Player.CanBeReady()) EnterQueueButt.BeginStoryboard(App.FadeIn);
      else EnterQueueButt.BeginStoryboard(App.FadeOut);
    }

    private void Player_MasteryClicked(object src, EventArgs args) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
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
      Player.UpdateBooks();
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      Player.CapPlayer.Champion = Popup.ChampSelector.SelectedChampion;
      Player.Skin = e;
      Popup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) Player.CapPlayer.Spell1 = spell;
      else Player.CapPlayer.Spell2 = spell;
      Popup.BeginStoryboard(App.FadeOut);
    }

    private void EnterQueue(object sender, RoutedEventArgs e) {
      var id = RiotCalls.CapService.CreateSoloQuery(Player.CapPlayer);
      RiotCalls.AddHandler(id, response => {
        if (response.status.Equals("OK"))
          Dispatcher.Invoke(() => Client.QueueManager.ShowQueuer(new CapSoloQueuer(Player.CapPlayer)));
      });
      if (Close != null) Close(this, new EventArgs());
    }

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => true;

    public void ForceClose() => Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
    public IQueuer HandleClose() {
      Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
      return null;
    }

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
