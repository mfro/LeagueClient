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
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderMe.xaml
  /// </summary>
  public partial class CapMePlayer : UserControl {
    public event EventHandler ChampClicked;
    public event EventHandler Spell1Clicked;
    public event EventHandler Spell2Clicked;
    public event EventHandler MasteryClicked;
    public event EventHandler PlayerUpdate;

    public CapPlayer State { get; set; }

    public CapMePlayer() {
      InitializeComponent();
      State = new CapPlayer();
      State.PlayerUpdate += Child_PlayerUpdate;
      PositionBox.ItemsSource = Strings.Position.Values.Values; //new string[] { "Top Lane", "Middle Lane", "Bottom Lane", "Jungle" };
      RoleBox.ItemsSource = Strings.Role.Values.Values; //new string[] { "Assassin", "Fighter", "Mage", "Marksman", "Support", "Tank" };
      RunesBox.ItemsSource = Client.LoginPacket.AllSummonerData.SpellBook.BookPages;
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
    }

    private void Child_PlayerUpdate(object sender, EventArgs e) {
      if (State.Champion != null) ChampionImage.Source = LeagueData.GetChampIconImage(State.Champion.id);
      if (State.Spell1 != null) Spell1Image.Source = LeagueData.GetSpellImage(State.Spell1.id);
      if (State.Spell2 != null) Spell2Image.Source = LeagueData.GetSpellImage(State.Spell2.id);
      if (State.Position != null) PositionBox.SelectedItem = State.Position;
      if (State.Role != null) RoleBox.SelectedItem = State.Role;
      if (PlayerUpdate != null) PlayerUpdate(this, e);
    }

    public void UpdateMasteries() {
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
    }

    public bool CanBeReady() {
      return State.CanBeReady() && RunesBox.SelectedIndex > 0 && MasteriesBox.SelectedIndex > 0;
    }

    private void Champion_Click(object src, EventArgs args) {
      if (ChampClicked != null) ChampClicked(this, new EventArgs());
    }

    private void Spell1_Click(object src, EventArgs args) {
      if (Spell1Clicked != null) Spell1Clicked(this, new EventArgs());
    }

    private void Spell2_Click(object src, EventArgs args) {
      if (Spell2Clicked != null) Spell2Clicked(this, new EventArgs());
    }

    private void Button_Click(object src, EventArgs args) {
      if (MasteryClicked != null) MasteryClicked(this, new EventArgs());
    }

    private void Runes_Selected(object sender, SelectionChangedEventArgs e) {
      var page = (SpellBookPageDTO) RunesBox.SelectedItem;
      RiotCalls.SpellBookService.SelectDefaultSpellBookPage(page);
    }

    private void Mastery_Selected(object sender, SelectionChangedEventArgs e) {
      var page = (MasteryBookPageDTO) MasteriesBox.SelectedItem;
      RiotCalls.MasteryBookService.SelectDefaultMasteryBookPage(page);
    }

    private void Position_Selected(object sender, SelectionChangedEventArgs e) {
      State.Position = (Position) PositionBox.SelectedItem;
    }

    private void Role_Selected(object sender, SelectionChangedEventArgs e) {
      State.Role = (Role) RoleBox.SelectedItem;
    }
  }
}
