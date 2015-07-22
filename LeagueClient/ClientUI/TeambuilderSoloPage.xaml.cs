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

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for TeambuilderSoloPage.xaml
  /// </summary>
  public partial class TeambuilderSoloPage : Page {
    private Image choosingSpell;

    private SpellDto spell1;
    private SpellDto spell2;
    private ChampionDto champ;
    private ChampionDto.SkinDto skin;

    public TeambuilderSoloPage() {
      InitializeComponent();
      PositionBox.ItemsSource = new string[] { "Top Lane", "Middle Lane", "Bottom Lane", "Jungle" };
      RoleBox.ItemsSource = new string[] { "Assassin", "Fighter", "Mage", "Marksman", "Support", "Tank" };
      ChampSelector.Champions = Client.AvailableChampions;
      SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                              where spell.modes.Contains("CLASSIC")
                              select spell);
      RunesBox.ItemsSource = Client.LoginPacket.AllSummonerData.SpellBook.BookPages;
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
      UpdateReadyState();
    }

    private void Champion_Click(object sender, MouseButtonEventArgs e) {
      ChampSelector.UpdateChampList();
      ChampPopup.BeginStoryboard(App.FadeIn);
      ChampSelector.Visibility = System.Windows.Visibility.Visible;
      MasteryEditor.Visibility = System.Windows.Visibility.Collapsed;
    }

    private void Spell_Click(object sender, MouseButtonEventArgs e) {
      var border = (Border) sender;
      switch (border.Name) {
        case "Spell1Border": choosingSpell = Spell1Image; break;
        case "Spell2Border": choosingSpell = Spell2Image; break;
      }
      SpellPopup.BeginStoryboard(App.FadeIn);
    }

    private void ChampionPopup_Close(object sender, RoutedEventArgs e) {
      ChampPopup.BeginStoryboard(App.FadeOut);
      MasteryEditor.Save().Wait();
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      champ = ChampSelector.SelectedChampion;
      skin = e;
      ChampionImage.Source = LeagueData.GetChampIconImage(ChampSelector.SelectedChampion.id);
      ChampionPopup_Close(sender, null);
      UpdateReadyState();
    }

    private void Spell_Select(object sender, SpellDto spell) {
      choosingSpell.Source = LeagueData.GetSpellImage(spell.id);
      SpellPopup.BeginStoryboard(App.FadeOut);
      switch (choosingSpell.Name) {
        case "Spell1Image": spell1 = spell; break;
        case "Spell2Image": spell2 = spell; break;
        default: Console.WriteLine("SHADIJSADA"); break;
      }
      UpdateReadyState();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      ChampPopup.BeginStoryboard(App.FadeIn);
      ChampSelector.Visibility = System.Windows.Visibility.Collapsed;
      MasteryEditor.Visibility = System.Windows.Visibility.Visible;
    }

    private void Box_Changed(object sender, SelectionChangedEventArgs e) {
      UpdateReadyState();
    }

    private void UpdateReadyState() {
      Controls.TeambuilderMap.Position pos;
      if (champ != null && PositionBox.SelectedIndex >= 0) {
        switch ((string) PositionBox.SelectedItem) {
          case "Top Lane": pos = Controls.TeambuilderMap.Position.Top; break;
          case "Middle Lane": pos = Controls.TeambuilderMap.Position.Mid; break;
          case "Bottom Lane": pos = Controls.TeambuilderMap.Position.Bot; break;
          case "Jungle": pos = Controls.TeambuilderMap.Position.Jungle; break;
          default: pos = Controls.TeambuilderMap.Position.Mid; break;
        }
        GameMap.Players.Clear();
        GameMap.Players.Add(new Controls.TeambuilderMap.Player { Champion = champ, Position = pos });
      }

      bool ready = true;
      if (champ == null || skin == null || spell1 == null || spell2 == null) ready = false;
      if (PositionBox.SelectedIndex < 0 || RoleBox.SelectedIndex < 0
        || RunesBox.SelectedIndex < 0 || MasteriesBox.SelectedIndex < 0) ready = false;
      if (ready) EnterQueueButt.Visibility = System.Windows.Visibility.Visible;
      else EnterQueueButt.Visibility = System.Windows.Visibility.Collapsed;
    }

    private void EnterQueue(object sender, RoutedEventArgs e) {
      //TODO Teambuilder SoloQ
    }
  }
}
