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
  public partial class TeambuilderSoloPage : Page {
    private bool spell1;

    public TeambuilderSoloPage() {
      InitializeComponent();
      ChampSelector.Champions = Client.AvailableChampions;
      SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                              where spell.modes.Contains("CLASSIC")
                              select spell);

      Player.ChampClicked += Champion_Click;
      Player.Spell1Clicked += Spell1_Click;
      Player.Spell2Clicked += Spell2_Click;
      Player.ReadyStateChanged += Player_ReadyStateChanged;
      Player.MasteryClicked += Player_MasteryClicked;
      Player.PositionChanged += Player_PositionChanged;
    }

    private void Player_PositionChanged() {
      GameMap.Players.Clear();
      GameMap.Players.Add(new Controls.TeambuilderMap.Player { Champion = Player.Champ, Position = Player.Position });
    }

    private void Player_MasteryClicked() {
      ChampPopup.BeginStoryboard(App.FadeIn);
      ChampSelector.Visibility = System.Windows.Visibility.Collapsed;
      MasteryEditor.Visibility = System.Windows.Visibility.Visible;
    }

    private void Spell1_Click() {
      spell1 = true;
      SpellPopup.BeginStoryboard(App.FadeIn);
    }

    private void Spell2_Click() {
      spell1 = false;
      SpellPopup.BeginStoryboard(App.FadeIn);
    }

    private void Champion_Click() {
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
      Player.Champ = ChampSelector.SelectedChampion;
      Player.Skin = e;
      ChampionPopup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) {
        Player.Spell1 = spell;
      } else {
        Player.Spell2 = spell;
      }
      SpellPopup.BeginStoryboard(App.FadeOut);
    }

    private void Player_ReadyStateChanged(bool ready) {
      if (ready) EnterQueueButt.BeginStoryboard(App.FadeIn);
      else EnterQueueButt.BeginStoryboard(App.FadeOut);
    }

    private void EnterQueue(object sender, RoutedEventArgs e) {
      Client.MainPage.EnterTeambuilderSolo(Player.Champ, Player.Position, Player.Role, Player.Spell1, Player.Spell2);
    }
  }
}
