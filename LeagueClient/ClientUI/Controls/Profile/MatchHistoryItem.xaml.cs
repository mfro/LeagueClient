using LeagueClient.Logic;
using LeagueClient.Logic.Riot;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for MatchHistoryItem.xaml
  /// </summary>
  public partial class MatchHistoryItem : UserControl {
    public MatchHistoryItem() {
      InitializeComponent();

      Loaded += (src, e) => {
        var game = DataContext as RiotACS.Game;
        var identity = game.ParticipantIdentities.FirstOrDefault(i => i.Player.AccountId == Client.LoginPacket.AllSummonerData.Summoner.AccountId);
        var me = game.Participants.FirstOrDefault(p => p.participantId == identity.ParticipantId);

        var champ = LeagueData.GetChampData(me.championId);
        var spell1 = LeagueData.GetSpellData(me.spell1Id);
        var spell2 = LeagueData.GetSpellData(me.spell2Id);

        ChampImage.Source = LeagueData.GetChampIconImage(champ);
        Spell1Image.Source = LeagueData.GetSpellImage(spell1);
        Spell2Image.Source = LeagueData.GetSpellImage(spell2);

        MapLabel.Content = LeagueData.GameMaps[game.MapId];
        if (game.GameType.Equals("CUSTOM_GAME")) {
          ModeLabel.Content = "Custom";
        } else if (game.GameType.Equals("TUTORIAL_GAME")) {
          ModeLabel.Content = "Tutorial";
        } else {
          ModeLabel.Content = GameMode.Values[game.GameMode].Value;
        }

        var items = new[] { me.stats.item0, me.stats.item1, me.stats.item2, me.stats.item3, me.stats.item4, me.stats.item5, me.stats.item6 };
        var images = new[] { Item0Image, Item1Image, Item2Image, Item3Image, Item4Image, Item5Image, Item6Image };
        for (int i = 0; i < items.Length; i++) {
          if (items[i] == 0) continue;
          var data = LeagueData.ItemData.Value.data[items[i].ToString()];
          images[i].Source = LeagueData.GetItemImage(data);
        }

        ScoreLabel.Content = $"{me.stats.kills} / {me.stats.deaths} / {me.stats.assists}";
        var date = Client.Epoch.AddMilliseconds(game.GameCreation);

        DateLabel.Content = date.ToString("M / d / yyyy");
        TimeLabel.Content = date.ToString("h:mm tt");
      };
    }
  }
}
