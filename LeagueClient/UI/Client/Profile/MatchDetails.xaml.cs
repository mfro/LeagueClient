using LeagueClient.Logic;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MFroehlich.League.RiotAPI;
using RiotClient;
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

namespace LeagueClient.UI.Client.Profile{
  /// <summary>
  /// Interaction logic for MatchDetails.xaml
  /// </summary>
  public partial class MatchDetails : UserControl {
    private Action back;
    public MatchDetails(RiotACS.Game game, RiotACS.Delta delta, Action back = null) {
      InitializeComponent();

      this.back = back;
      if (back == null) BackButton.Visibility = Visibility.Collapsed;

      var blue = new List<object>();
      var red = new List<object>();
      foreach (var player in game.Participants) {
        var champ = DataDragon.GetChampData(player.ChampionId);
        var spell1 = DataDragon.GetSpellData(player.Spell1Id);
        var spell2 = DataDragon.GetSpellData(player.Spell2Id);

        var items = new[] { player.Stats.Item0, player.Stats.Item1, player.Stats.Item2, player.Stats.Item3, player.Stats.Item4, player.Stats.Item5, player.Stats.Item6 };

        var item = new {
          ChampImage = DataDragon.GetChampIconImage(champ),
          Spell1Image = DataDragon.GetSpellImage(spell1),
          Spell2Image = DataDragon.GetSpellImage(spell2),
          Name = champ.name,
          Score = $"{player.Stats.Kills} / {player.Stats.Deaths} / {player.Stats.Assists}",
          Item0Image = DataDragon.GetItemImage(items[0]),
          Item1Image = DataDragon.GetItemImage(items[1]),
          Item2Image = DataDragon.GetItemImage(items[2]),
          Item3Image = DataDragon.GetItemImage(items[3]),
          Item4Image = DataDragon.GetItemImage(items[4]),
          Item5Image = DataDragon.GetItemImage(items[5]),
          Item6Image = DataDragon.GetItemImage(items[6]),
          CS = player.Stats.TotalMinionsKilled,
          Gold = (player.Stats.GoldEarned / 1000.0).ToString("#.#k")
        };

        if (player.TeamId == 100) blue.Add(item);
        else red.Add(item);
      }
      BlueTeam.ItemsSource = blue;
      RedTeam.ItemsSource = red;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      back();
    }

    private void OverviewButton_Click(object sender, RoutedEventArgs e) {

    }

    private void GraphsButton_Click(object sender, RoutedEventArgs e) {

    }
  }
}
