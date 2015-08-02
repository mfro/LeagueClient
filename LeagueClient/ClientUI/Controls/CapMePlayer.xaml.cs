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

    public CapPlayer CapPlayer { get; set; }
    public ChampionDto.SkinDto Skin { get; set; }

    private bool editable;

    public CapMePlayer() : this(null) { }

    public CapMePlayer(CapPlayer player) {
      CapPlayer = player ?? new CapPlayer();
      CapPlayer.PropertyChanged += (s, e) => Dispatcher.Invoke(UpdateChild);

      InitializeComponent();
      PositionBox.ItemsSource = Strings.Position.Values.Values; //new string[] { "Top Lane", "Middle Lane", "Bottom Lane", "Jungle" };
      RoleBox.ItemsSource = Strings.Role.Values.Values.Where(p => p != Role.ANY); //new string[] { "Assassin", "Fighter", "Mage", "Marksman", "Support", "Tank" };
      RunesBox.ItemsSource = Client.Runes.BookPages;
      RunesBox.SelectedItem = Client.SelectedRunePage;
      Check.Visibility = Visibility.Collapsed;
      CapPlayer.Status = CapStatus.Present;
      UpdateMasteries();
    }

    private void UpdateChild() {
      Dispatcher.Invoke(() => {
        if (CapPlayer.Champion != null) ChampionImage.Source = LeagueData.GetChampIconImage(CapPlayer.Champion.id);
        if (CapPlayer.Spell1 != null) Spell1Image.Source = LeagueData.GetSpellImage(CapPlayer.Spell1.id);
        if (CapPlayer.Spell2 != null) Spell2Image.Source = LeagueData.GetSpellImage(CapPlayer.Spell2.id);
        if (CapPlayer.Position != null) {
          PositionBox.SelectedItem = CapPlayer.Position;
          PositionText.Text = CapPlayer.Position.Value;
        }
        if (CapPlayer.Role != null) {
          RoleBox.SelectedItem = CapPlayer.Role;
          RoleText.Text = CapPlayer.Role.Value;
        }
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
        Check.Visibility = (CapPlayer.Status == CapStatus.Ready) ? Visibility.Visible : Visibility.Collapsed;
      });
    }

    public void UpdateMasteries() {
      MasteriesBox.ItemsSource = Client.Masteries.BookPages;
      MasteriesBox.SelectedItem = Client.SelectedMasteryPage;
    }

    public bool CanBeReady() {
      return CapPlayer.CanBeReady() && RunesBox.SelectedIndex >= 0 && MasteriesBox.SelectedIndex >= 0;
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
      CapPlayer.Position = (Position) PositionBox.SelectedItem;
    }

    private void Role_Selected(object sender, SelectionChangedEventArgs e) {
      CapPlayer.Role = (Role) RoleBox.SelectedItem;
    }
  }
}
