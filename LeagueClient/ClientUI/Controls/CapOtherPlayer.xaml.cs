﻿using System;
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
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.Parsing.DynamicJSON;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderPlayer.xaml
  /// </summary>
  public partial class CapOtherPlayer : UserControl {
    public CapPlayer State { get; private set; }

    public CapOtherPlayer(CapPlayer player) {
      InitializeComponent();
      State = player;
      State.PropertyChanged += (s, e) => Dispatcher.Invoke(PlayerUpdate);
      Unknown.Visibility = Visibility.Collapsed;
      PlayerUpdate();
    }

    private void PlayerUpdate() {
      if (State.Position != null) {
        RoleText.Text = State.Position.Value;
        if (State.Role != null) RoleText.Text += " / " + State.Role.Value;
      } else if (State.Role != null) {
        RoleText.Text = State.Role.Value;
      } else RoleText.Text = "";

      ChampImage.Source = null;
      Spell1Image.Source = null;
      Spell2Image.Source = null;
      Check.Visibility = Visibility.Collapsed;
      switch (State.Status) {
        case CapStatus.Ready:
          Check.Visibility = Visibility.Visible;
          goto case CapStatus.Present;
        case CapStatus.Present:
          SummonerText.Text = State.Name;
          UpdateImages();
          break;
        case CapStatus.Penalty:
          SummonerText.Text = "Player kicked.";
          break;
        case CapStatus.Searching:
          SummonerText.Text = "Searching for candidate...";
          break;
        case CapStatus.SearchingDeclined:
          SummonerText.Text = "The player was not found, searching for another candidate...";
          break;
        case CapStatus.Maybe:
          SummonerText.Text = "A candidate has been found.";
          UpdateImages();
          break;
        case CapStatus.Choosing:
          break;//TODO
      }
    }

    private void UpdateImages() {
      if (State.Champion != null) ChampImage.Source = LeagueData.GetChampIconImage(State.Champion.id);
      if (State.Spell1 != null) Spell1Image.Source = LeagueData.GetSpellImage(State.Spell1.id);
      if (State.Spell2 != null) Spell2Image.Source = LeagueData.GetSpellImage(State.Spell2.id);
    }
  }
}
