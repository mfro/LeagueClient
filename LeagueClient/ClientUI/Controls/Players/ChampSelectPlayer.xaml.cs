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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChampSelectPlayer.xaml
  /// </summary>
  public partial class ChampSelectPlayer : UserControl {

    public ChampSelectPlayer() {
      InitializeComponent();
    }

    public ChampSelectPlayer(PlayerParticipant player, PlayerChampionSelectionDTO selection) : this() {
      if (selection?.ChampionId > 0)
        ChampImage.Source = LeagueData.GetChampIconImage(LeagueData.GetChampData(selection.ChampionId));
      else ChampImage.Source = null;

      if (selection?.Spell1Id > 0)
        Spell1Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell1Id));
      else Spell1Image.Source = null;

      if (selection?.Spell2Id > 0)
        Spell2Image.Source = LeagueData.GetSpellImage(LeagueData.GetSpellData(selection.Spell2Id));
      else Spell2Image.Source = null;

      NameLabel.Content = player.SummonerName;
    }

    public ChampSelectPlayer(BotParticipant bot) : this() {
      var champ = LeagueData.ChampData.Value.data[bot.SummonerInternalName.Split('_')[1]];
      ChampImage.Source = LeagueData.GetChampIconImage(champ);
      NameLabel.Content = champ.name;
    }

    public ChampSelectPlayer(ObfuscatedParticipant obfusc) : this() {
      ChampImage.Source = Spell1Image.Source = Spell2Image.Source = null;
      NameLabel.Visibility = Visibility.Collapsed;
      Unknown.Visibility = Visibility.Visible;
    }
  }
}
