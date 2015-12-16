using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
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

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for PlayerProfile.xaml
  /// </summary>
  public partial class PlayerProfile : UserControl {
    private BindingList<SummonerCache.Item> history = new BindingList<SummonerCache.Item>();
    private SummonerCache.Item Selected;

    public PlayerProfile() {
      InitializeComponent();

      if (Client.Connected) {
        HistoryList.ItemsSource = history;
        Client.SummonerCache.GetData(Client.LoginPacket.AllSummonerData.Summoner.Name, GotSummoner);
      }
    }

    public void GotSummoner(SummonerCache.Item item) {
      if (item == null) {
        Dispatcher.Invoke(() => SearchBox.BorderBrush = App.AwayBrush);
      } else if (!history.Contains(item)) {
        if (history.Count == 0)
          history.Add(item);
        else
          history.Insert(1, item);
        Dispatcher.MyInvoke(LoadSummoner, item);
      }
    }

    private void LoadSummoner(SummonerCache.Item item) {
      SearchBox.BorderBrush = App.ForeBrush;
      HistoryList.SelectedItem = Selected = item;
      SummonerName.Content = item.Data.Summoner.Name;
      SummonerIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(item.Data.Summoner.ProfileIconId));

      if (item.Data.SummonerLevel.Level < 30) {
        SummonerRank.Content = "Level " + item.Data.SummonerLevel;
      } else {
        var league = item.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
        if (league != null) SummonerRank.Content = RankedTier.Values[league.Tier] + " " + league.Rank;
      }
    }

    private void SearchBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter && SearchBox.Text.Trim().Length > 0) {
        Client.SummonerCache.GetData(SearchBox.Text, GotSummoner);
      }
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      LoadSummoner((SummonerCache.Item) HistoryList.SelectedItem);
    }

    private void TabList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (DetailsPane == null) return;
      Console.WriteLine(TabList.SelectedItem == ProfileTab);
      if (TabList.SelectedItem == MatchHistoryTab) {
        var history = new MatchHistory(Selected);
        DetailsPane.Child = history;
      } else if (TabList.SelectedItem == RankingTab) {
        //TODO Ranking
        DetailsPane.Child = null;
      } else if (TabList.SelectedItem == ProfileTab) {
        //TODO Profile
        DetailsPane.Child = null;
      }
    }
  }
}
