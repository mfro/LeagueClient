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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using LeagueClient.Logic;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChampSelectPlayer.xaml
  /// </summary>
  public partial class ChampSelectPlayer : UserControl {

    public ChampSelectPlayer() {
      InitializeComponent();
    }

    public ChampSelectPlayer(PlayerParticipant player, PlayerChampionSelectionDTO selection) : this() {
      if (player.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId)
        Glow.Opacity = 1;

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

    private void DisplaySelection(PlayerChampionSelectionDTO selection) {
      if (selection?.Spell1Id > 0 && selection?.Spell2Id > 0 && selection?.ChampionId > 0) {
        ChampImage.Source = LeagueData.GetChampIconImage(LeagueData.GetChampData(selection.ChampionId));
        Spell1Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell1Id));
        Spell2Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell2Id));
        Unknown.Visibility = Obscure.Visibility = Visibility.Collapsed;
      } else if (selection?.Spell1Id > 0 && selection?.Spell2Id > 0) {
        Spell1Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell1Id));
        Spell2Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell2Id));
        Grid.SetColumnSpan(Unknown, 1);
        Grid.SetColumnSpan(Obscure, 1);
        Unknown.Visibility = Obscure.Visibility = Visibility.Visible;
      } else {
        Unknown.Visibility = Obscure.Visibility = Visibility.Visible;
      }
    }

    public ChampSelectPlayer(BotParticipant bot) : this() {
      var champ = LeagueData.ChampData.Value.data[bot.SummonerInternalName.Split('_')[1]];
      ChampImage.Source = LeagueData.GetChampIconImage(champ);
      NameLabel.Visibility = Visibility.Visible;
      NameLabel.Content = champ.name;
      Unknown.Visibility = Obscure.Visibility = Visibility.Collapsed;
    }
  }
}
