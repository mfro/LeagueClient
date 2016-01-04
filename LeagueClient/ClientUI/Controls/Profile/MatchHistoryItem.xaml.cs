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
        var identity = game.ParticipantIdentities.FirstOrDefault(i => i.Player.AccountId == Client.Session.LoginPacket.AllSummonerData.Summoner.AccountId);
        var me = game.Participants.FirstOrDefault(p => p.ParticipantId == identity.ParticipantId);

        var champ = LeagueData.GetChampData(me.ChampionId);
        var spell1 = LeagueData.GetSpellData(me.Spell1Id);
        var spell2 = LeagueData.GetSpellData(me.Spell2Id);

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

        var items = new[] { me.Stats.Item0, me.Stats.Item1, me.Stats.Item2, me.Stats.Item3, me.Stats.Item4, me.Stats.Item5, me.Stats.Item6 };
        var images = new[] { Item0Image, Item1Image, Item2Image, Item3Image, Item4Image, Item5Image, Item6Image };
        for (int i = 0; i < items.Length; i++) {
          images[i].Source = LeagueData.GetItemImage(items[i]);
        }

        ScoreLabel.Content = $"{me.Stats.Kills} / {me.Stats.Deaths} / {me.Stats.Assists}";
        var date = Client.Epoch.AddMilliseconds(game.GameCreation);

        DateLabel.Content = date.ToString("M / d / yyyy");
        TimeLabel.Content = date.ToString("h:mm tt");
      };
    }
  }
}
