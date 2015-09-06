﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public CapMePlayer(CapPlayer player) {
      CapPlayer = player ?? new CapPlayer (-1) { Status = CapStatus.Present };
      InitializeComponent();
      SummonerName.Text = Client.LoginPacket.AllSummonerData.Summoner.Name;

      CapPlayer.PropertyChanged += (s, e) => Dispatcher.MyInvoke(UpdateChild, e.PropertyName);
      PositionBox.ItemsSource = Position.Values.Values.Where(p => p != Position.UNSELECTED);
      RoleBox.ItemsSource = Role.Values.Values.Where(p => p != Role.ANY && p != Role.UNSELECTED);
      UpdateBooks();
      var spells = Client.LoginPacket.AllSummonerData.SummonerDefaultSpells.SummonerDefaultSpellMap["CLASSIC"];
      CapPlayer.Spell1 = LeagueData.GetSpellData(spells.Spell1Id);
      CapPlayer.Spell2 = LeagueData.GetSpellData(spells.Spell2Id);
    }

    private void UpdateChild(string property) {
      PlayerUpdate?.Invoke(this, new EventArgs());
      ChampionName.Text = CapPlayer.Champion?.name;
      Check.Visibility = (CapPlayer.Status == CapStatus.Ready) ? Visibility.Visible : Visibility.Collapsed;
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
      Client.SelectRunePage((SpellBookPageDTO) RunesBox.SelectedItem);
      if (PlayerUpdate != null) PlayerUpdate(this, e);
    }

    private void Mastery_Selected(object sender, SelectionChangedEventArgs e) {
      Client.SelectMasteryPage((MasteryBookPageDTO) MasteriesBox.SelectedItem);
      if (PlayerUpdate != null) PlayerUpdate(this, e);
    }
  }
}