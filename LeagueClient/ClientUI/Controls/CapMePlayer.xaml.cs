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

    public bool Editable {
      get {
        return editable;
      }
      set {
        if (value) {
          ChampionBorder.Cursor = Spell1Border.Cursor = Spell2Border.Cursor = Cursors.Hand;
          PositionBox.Visibility = RoleBox.Visibility = PositionLabel.Visibility = RoleLabel.Visibility = Visibility.Visible;
        } else {
          ChampionBorder.Cursor = Spell1Border.Cursor = Spell2Border.Cursor = Cursors.Hand;
          PositionBox.Visibility = RoleBox.Visibility = PositionLabel.Visibility = RoleLabel.Visibility = Visibility.Collapsed;
        }
        editable = value;
      }
    }

    public CapPlayer State { get; set; }
    public ChampionDto.SkinDto Skin { get; set; }

    private bool editable;

    public CapMePlayer() {
      InitializeComponent();
      State = new CapPlayer();
      State.Name = Client.LoginPacket.AllSummonerData.Summoner.Name;
      State.PlayerUpdate += Child_PlayerUpdate;
      PositionBox.ItemsSource = Strings.Position.Values.Values; //new string[] { "Top Lane", "Middle Lane", "Bottom Lane", "Jungle" };
      RoleBox.ItemsSource = Strings.Role.Values.Values.Where(p => p != Role.ANY); //new string[] { "Assassin", "Fighter", "Mage", "Marksman", "Support", "Tank" };
      RunesBox.ItemsSource = Client.Runes.BookPages;
      RunesBox.SelectedItem = Client.SelectedRunePage;
      Check.Visibility = Visibility.Collapsed;
      State.Status = CapStatus.Present;
      UpdateMasteries();
    }

    private void Child_PlayerUpdate(object sender, EventArgs e) {
      if (State.Champion != null) ChampionImage.Source = LeagueData.GetChampIconImage(State.Champion.id);
      if (State.Spell1 != null) Spell1Image.Source = LeagueData.GetSpellImage(State.Spell1.id);
      if (State.Spell2 != null) Spell2Image.Source = LeagueData.GetSpellImage(State.Spell2.id);
      if (State.Position != null) {
        PositionBox.SelectedItem = State.Position;
        PositionText.Text = State.Position.Value;
      }
      if (State.Role != null) {
        RoleBox.SelectedItem = State.Role;
        RoleText.Text = State.Role.Value;
      }
      if (PlayerUpdate != null) PlayerUpdate(this, e);
      Check.Visibility = (State.Status == CapStatus.Ready) ? Visibility.Visible : Visibility.Collapsed;
    }

    public void UpdateMasteries() {
      MasteriesBox.ItemsSource = Client.Masteries.BookPages;
      MasteriesBox.SelectedItem = Client.SelectedMasteryPage;
    }

    public bool CanBeReady() {
      return State.CanBeReady() && RunesBox.SelectedIndex >= 0 && MasteriesBox.SelectedIndex >= 0;
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
      if (page == Client.SelectedRunePage) return;
      Client.SelectRunePage(page);
      if (PlayerUpdate != null) PlayerUpdate(this, e);
    }

    private void Mastery_Selected(object sender, SelectionChangedEventArgs e) {
      var page = (MasteryBookPageDTO) MasteriesBox.SelectedItem;
      if (page == Client.SelectedMasteryPage) return;
      Client.SelectMasteryPage(page);
      if (PlayerUpdate != null) PlayerUpdate(this, e);
    }

    private void Position_Selected(object sender, SelectionChangedEventArgs e) {
      State.Position = (Position) PositionBox.SelectedItem;
    }

    private void Role_Selected(object sender, SelectionChangedEventArgs e) {
      State.Role = (Role) RoleBox.SelectedItem;
    }
  }
}
