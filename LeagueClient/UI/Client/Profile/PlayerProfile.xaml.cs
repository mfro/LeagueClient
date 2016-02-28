using LeagueClient.Logic;
using MFroehlich.League.Assets;
using RiotClient;
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

namespace LeagueClient.UI.Client.Profile {
  /// <summary>
  /// Interaction logic for PlayerProfile.xaml
  /// </summary>
  public partial class PlayerProfile : UserControl {
    private BindingList<SummonerCache.Item> history = new BindingList<SummonerCache.Item>();
    private SummonerCache.Item Selected;

    public PlayerProfile() {
      InitializeComponent();

      if (Session.Current != null) {
        HistoryList.ItemsSource = history;
        Session.Current.SummonerCache.GetData(Session.Current.Account.Name, GotSummoner);
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

      if (item.Data != null) {
        SummonerName.Content = item.Data.Summoner.Name;
        SummonerIcon.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(item.Data.Summoner.ProfileIconId)).Load();
      }

      if (item.MatchHistory != null) {
        MatchesPane.Child = new MatchHistory(item);
      }

      if (item.Data != null && item.Data.SummonerLevel.Level < 30) {
        SummonerRank.Content = "Level " + item.Data.SummonerLevel;
      } else if (item.Leagues != null) {
        var league = item.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
        if (league != null) SummonerRank.Content = RankedTier.Values[league.Tier] + " " + league.Rank;
      }
      Profile_Click(ProfileTab, null);
    }

    private void SearchBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter && SearchBox.Text.Trim().Length > 0) {
        Session.Current.SummonerCache.GetData(SearchBox.Text, GotSummoner);
      }
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      LoadSummoner((SummonerCache.Item) HistoryList.SelectedItem);
    }

    private void Matches_Click(object sender, MouseButtonEventArgs e) {
      MatchesTab.Foreground = Brushes.White;
      ProfileTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);
      RankingTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);

      MatchesPane.Visibility = Visibility.Visible;
      RankingPane.Visibility = ProfilePane.Visibility = Visibility.Collapsed;
    }

    private void Ranking_Click(object sender, MouseButtonEventArgs e) {
      RankingTab.Foreground = Brushes.White;
      ProfileTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);
      MatchesTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);

      RankingPane.Visibility = Visibility.Visible;
      MatchesPane.Visibility = ProfilePane.Visibility = Visibility.Collapsed;
    }

    private void Profile_Click(object sender, MouseButtonEventArgs e) {
      ProfileTab.Foreground = Brushes.White;
      RankingTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);
      MatchesTab.SetValue(ForegroundProperty, DependencyProperty.UnsetValue);

      ProfilePane.Visibility = Visibility.Visible;
      RankingPane.Visibility = MatchesPane.Visibility = Visibility.Collapsed;
    }
  }
}
