using LeagueClient.Logic;
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

namespace LeagueClient.UI.Client.Profile {
  /// <summary>
  /// Interaction logic for MatchHistory.xaml
  /// </summary>
  public partial class MatchHistory : UserControl {
    public MatchHistory(SummonerCache.Item item) {
      InitializeComponent();

      MatchList.ItemsSource = item.MatchHistory.Games.Games.OrderByDescending(game => game.GameCreation);
    }

    private async void MatchHistoryItem_MouseUp(object sender, MouseButtonEventArgs e) {
      var game = ((MatchHistoryItem) sender).DataContext as RiotACS.Game;
      if (game != null) {
        var details = RiotACS.GetMatchDetails(Session.Region.Platform, game.GameId);
        var deltas = await RiotACS.GetDeltas();
        var delta = deltas.Deltas.FirstOrDefault(d => d.GameId == game.GameId).Delta;

        Details.Child = new MatchDetails(await details, delta, () => Details.Child = null);
      }
    }
  }
}
