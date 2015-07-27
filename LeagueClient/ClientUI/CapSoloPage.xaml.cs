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
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for TeambuilderSoloPage.xaml
  /// </summary>
  public partial class CapSoloPage : Page {
    private bool spell1;

    public CapSoloPage() {
      InitializeComponent();
      ChampSelector.Champions = Client.AvailableChampions;
      SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                              where spell.modes.Contains("CLASSIC")
                              select spell);

      Player.PlayerUpdate += PlayerUpdate;
      Player.ChampClicked += Champion_Click;
      Player.Spell1Clicked += Spell1_Click;
      Player.Spell2Clicked += Spell2_Click;
      Player.MasteryClicked += Player_MasteryClicked;
    }

    private void PlayerUpdate(object sender, EventArgs e) {
      GameMap.Players.Clear();
      GameMap.Players.Add(new Controls.CapMap.Player { Champion = Player.State.Champion, Position = Player.State.Position });
      if (Player.CanBeReady()) EnterQueueButt.BeginStoryboard(App.FadeIn);
      else EnterQueueButt.BeginStoryboard(App.FadeOut);
    }

    private void Player_MasteryClicked(object src, EventArgs args) {
      ChampPopup.BeginStoryboard(App.FadeIn);
      MasteryEditor.Reset();
      ChampSelector.Visibility = System.Windows.Visibility.Collapsed;
      MasteryEditor.Visibility = System.Windows.Visibility.Visible;
    }

    private void Spell1_Click(object src, EventArgs args) {
      spell1 = true;
      SpellPopup.BeginStoryboard(App.FadeIn);
    }

    private void Spell2_Click(object src, EventArgs args) {
      spell1 = false;
      SpellPopup.BeginStoryboard(App.FadeIn);
    }

    private void Champion_Click(object src, EventArgs args) {
      ChampSelector.UpdateChampList();
      ChampPopup.BeginStoryboard(App.FadeIn);
      ChampSelector.Visibility = System.Windows.Visibility.Visible;
      MasteryEditor.Visibility = System.Windows.Visibility.Collapsed;
    }

    private void ChampionPopup_Close(object sender, RoutedEventArgs e) {
      ChampPopup.BeginStoryboard(App.FadeOut);
      MasteryEditor.Save().Wait();
      Player.UpdateMasteries();
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      Player.State.Champion = ChampSelector.SelectedChampion;
      Player.State.Skin = e;
      ChampionPopup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) {
        Player.State.Spell1 = spell;
      } else {
        Player.State.Spell2 = spell;
      }
      SpellPopup.BeginStoryboard(App.FadeOut);
    }

    private void EnterQueue(object sender, RoutedEventArgs e) {
      Client.QueueManager.EnterCapSolo(Player.State);
    }
  }
}
