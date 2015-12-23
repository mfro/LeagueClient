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
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using System.Threading;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderMe.xaml
  /// </summary>
  public partial class CapMePlayer : UserControl, IDisposable {
    public bool Editable {
      get {
        return editable;
      }
      set {
        if (value) {
          ChampionBorder.Cursor = Cursors.Hand;
          PositionBox.Visibility = RoleBox.Visibility = PositionLabel.Visibility = RoleLabel.Visibility = Visibility.Visible;
        } else {
          ChampionBorder.Cursor = Cursors.Arrow;
          PositionBox.Visibility = RoleBox.Visibility = PositionLabel.Visibility = RoleLabel.Visibility = Visibility.Collapsed;
        }
        editable = value;
      }
    }

    public CapPlayer CapPlayer { get; set; }
    public ChampionDto.SkinDto Skin { get; set; }

    private bool editable;

    public CapMePlayer() : this(null) { }

    public CapMePlayer(CapPlayer player, bool inLobby = false) {
      InitializeComponent();
      if (!Client.Connected) return;

      if (player == null) {
        CapPlayer = new CapPlayer(-1) { Status = CapStatus.Present };
        var spells = Client.LoginPacket.AllSummonerData.SummonerDefaultSpells.SummonerDefaultSpellMap["CLASSIC"];
        CapPlayer.Spell1 = LeagueData.GetSpellData(spells.Spell1Id);
        CapPlayer.Spell2 = LeagueData.GetSpellData(spells.Spell2Id);
      } else {
        CapPlayer = player;
      }
      if (!inLobby)
        CapPlayer.CapEvent += PlayerHandler;

      SummonerName.Text = Client.LoginPacket.AllSummonerData.Summoner.Name;
      PositionBox.ItemsSource = Position.Values.Values.Where(p => p != Position.UNSELECTED);
      RoleBox.ItemsSource = Role.Values.Values.Where(p => p != Role.ANY && p != Role.UNSELECTED);

      Client.PopupSelector.SpellSelector.SpellSelected += Spell_Select;
      Client.PopupSelector.ChampSelector.SkinSelected += ChampSelector_SkinSelected;

      UpdateBooks();

      Render();
    }

    private void Render() {
      ChampionName.Text = CapPlayer.Champion?.name;

      PositionBox.SelectedItem = CapPlayer.Position;
      RoleBox.SelectedItem = CapPlayer.Role;
      PositionText.Text = CapPlayer.Position?.Value;
      RoleText.Text = CapPlayer.Role?.Value;

      if (CapPlayer.Champion != null)
        ChampionImage.Source = LeagueData.GetChampIconImage(CapPlayer.Champion);
      if (CapPlayer.Spell1 != null)
        Spell1Image.Source = LeagueData.GetSpellImage(CapPlayer.Spell1);
      if (CapPlayer.Spell2 != null)
        Spell2Image.Source = LeagueData.GetSpellImage(CapPlayer.Spell2);

      Check.Visibility = (CapPlayer.Status == CapStatus.Ready) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PlayerHandler(object sender, CapPlayerEventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(PlayerHandler, sender, e); return; }
      var player = sender as CapPlayer;

      var change = e as PropertyChangedEventArgs;

      if (change != null) {
        switch (change.PropertyName) {
          case nameof(player.Position):
            player.Position = change.Value as Position;
            break;
          case nameof(player.Role):
            player.Role = change.Value as Role;
            break;
          case nameof(player.Champion):
            player.Champion = change.Value as ChampionDto;
            break;
          case nameof(player.Spell1):
            player.Spell1 = change.Value as SpellDto;
            break;
          case nameof(player.Spell2):
            player.Spell2 = change.Value as SpellDto;
            break;
        }
      }

      Render();
    }

    public void UpdateBooks() {
      RunesBox.ItemsSource = Client.Runes.BookPages;
      RunesBox.SelectedItem = Client.SelectedRunePage;
      MasteriesBox.ItemsSource = Client.Masteries.BookPages;
      MasteriesBox.SelectedItem = Client.SelectedMasteryPage;
    }

    public bool CanBeReady() {
      return CapPlayer.CanBeReady() && RunesBox.SelectedIndex >= 0 && MasteriesBox.SelectedIndex >= 0;
    }

    #region Editing

    private void Champion_Click(object src, EventArgs args) {
      Client.ShowPopup(PopupSelector.Selector.Champions);
    }
    private bool spell1;

    private void Spell1_Click(object src, EventArgs args) {
      spell1 = true;
      Client.ShowPopup(PopupSelector.Selector.Spells);
    }

    private void Spell2_Click(object src, EventArgs args) {
      spell1 = false;
      Client.ShowPopup(PopupSelector.Selector.Spells);
    }

    private void RuneEdit_Click(object sender, RoutedEventArgs e) {
      spell1 = false;
      Client.ShowPopup(PopupSelector.Selector.Runes);
    }

    private void MasteryEdit_Click(object src, RoutedEventArgs args) {
      spell1 = false;
      Client.ShowPopup(PopupSelector.Selector.Masteries);
    }

    private void Runes_Selected(object sender, SelectionChangedEventArgs e) {
      Client.SelectRunePage((SpellBookPageDTO) RunesBox.SelectedItem);
    }

    private void Mastery_Selected(object sender, SelectionChangedEventArgs e) {
      Client.SelectMasteryPage((MasteryBookPageDTO) MasteriesBox.SelectedItem);
    }

    private void PositionBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (CapPlayer.Position != PositionBox.SelectedItem)
        CapPlayer.ChangeProperty(nameof(CapPlayer.Position), (Position) PositionBox.SelectedItem);
    }

    private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (CapPlayer.Role != RoleBox.SelectedItem)
        CapPlayer.ChangeProperty(nameof(CapPlayer.Role), (Role) RoleBox.SelectedItem);
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      CapPlayer.ChangeProperty(nameof(CapPlayer.Champion), Client.PopupSelector.ChampSelector.SelectedChampion);
      Skin = e;
      Client.HidePopup();
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) CapPlayer.ChangeProperty(nameof(CapPlayer.Spell1), spell);
      else CapPlayer.ChangeProperty(nameof(CapPlayer.Spell2), spell);
      Client.HidePopup();
    }

    #endregion

    public void Dispose() {
      Client.PopupSelector.SpellSelector.SpellSelected -= Spell_Select;
      Client.PopupSelector.ChampSelector.SkinSelected -= ChampSelector_SkinSelected;
    }
  }
}
