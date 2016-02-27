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
using MFroehlich.League.Assets;
using LeagueClient.Logic;
using RiotClient.Riot.Platform;
using RiotClient;

namespace LeagueClient.UI.ChampSelect {
  /// <summary>
  /// Interaction logic for ChampSelectPlayer.xaml
  /// </summary>
  public partial class ChampSelectPlayer : UserControl {

    public ChampSelectPlayer() {
      InitializeComponent();
    }

    public ChampSelectPlayer(PlayerParticipant player, PlayerChampionSelectionDTO selection) : this() {
      if (player.SummonerId == Session.Current.Account.SummonerID)
        Glow.Opacity = 1;

      DisplaySelection(selection);

      NameLabel.Content = player.SummonerName;
      if (string.IsNullOrWhiteSpace(player.SummonerName))
        NameLabel.Visibility = Visibility.Collapsed;
      else
        NameLabel.Visibility = Visibility.Visible;
    }

    public ChampSelectPlayer(ObfuscatedParticipant obfusc, PlayerChampionSelectionDTO selection) : this() {
      DisplaySelection(selection);

      NameLabel.Visibility = Visibility.Collapsed;
    }

    public ChampSelectPlayer(BotParticipant bot) : this() {
      var champ = DataDragon.ChampData.Value.data[bot.SummonerInternalName.Split('_')[1]];
      ChampImage.Source = DataDragon.GetChampIconImage(champ).Load();
      NameLabel.Visibility = Visibility.Visible;
      NameLabel.Content = champ.name;
      Unknown.Visibility = Obscure.Visibility = Visibility.Collapsed;
    }

    private void DisplaySelection(PlayerChampionSelectionDTO selection) {
      if (selection?.Spell1Id > 0 && selection?.Spell2Id > 0 && selection?.ChampionId > 0) {
        ChampImage.Source = DataDragon.GetChampIconImage(DataDragon.GetChampData(selection.ChampionId)).Load();
        Spell1Image.Source = DataDragon.GetSpellImage(DataDragon.GetSpellData(selection.Spell1Id)).Load();
        Spell2Image.Source = DataDragon.GetSpellImage(DataDragon.GetSpellData(selection.Spell2Id)).Load();
        Unknown.Visibility = Obscure.Visibility = Visibility.Collapsed;
      } else if (selection?.Spell1Id > 0 && selection?.Spell2Id > 0) {
        Spell1Image.Source = DataDragon.GetSpellImage(DataDragon.GetSpellData(selection.Spell1Id)).Load();
        Spell2Image.Source = DataDragon.GetSpellImage(DataDragon.GetSpellData(selection.Spell2Id)).Load();
        Grid.SetColumnSpan(Unknown, 1);
        Grid.SetColumnSpan(Obscure, 1);
        Unknown.Visibility = Obscure.Visibility = Visibility.Visible;
      } else {
        Unknown.Visibility = Obscure.Visibility = Visibility.Visible;
      }
    }
  }
}
