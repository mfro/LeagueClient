using LeagueClient.Logic;
using MFroehlich.League.Assets;
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

namespace LeagueClient.UI.Client.Profile {
  /// <summary>
  /// Interaction logic for MatchHistoryItem.xaml
  /// </summary>
  public partial class MatchHistoryItem : UserControl {
    public MatchHistoryItem() {
      InitializeComponent();

      Loaded += (src, e) => {
        var game = DataContext as RiotACS.Game;
        var identity = game.ParticipantIdentities.FirstOrDefault(i => i.Player.AccountId == Session.Current.Account.AccountID);
        var me = game.Participants.FirstOrDefault(p => p.ParticipantId == identity.ParticipantId);

        var champ = DataDragon.GetChampData(me.ChampionId);
        var spell1 = DataDragon.GetSpellData(me.Spell1Id);
        var spell2 = DataDragon.GetSpellData(me.Spell2Id);

        ChampImage.Source = DataDragon.GetChampIconImage(champ).Load();
        Spell1Image.Source = DataDragon.GetSpellImage(spell1).Load();
        Spell2Image.Source = DataDragon.GetSpellImage(spell2).Load();

        MapLabel.Content = DataDragon.GameMaps[game.MapId];
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
          images[i].Source = DataDragon.GetItemImage(items[i]).Load();
        }

        ScoreLabel.Content = $"{me.Stats.Kills} / {me.Stats.Deaths} / {me.Stats.Assists}";
        var date = Session.Epoch.AddMilliseconds(game.GameCreation);

        DateLabel.Content = date.ToString("M / d / yyyy");
        TimeLabel.Content = date.ToString("h:mm tt");
      };
    }
  }
}
