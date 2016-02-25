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
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using agsXMPP.protocol.client;
using MFroehlich.League.DataDragon;

namespace LeagueClient.UI.Main.Friends {
  /// <summary>
  /// Interaction logic for Friend.xaml
  /// </summary>
  public partial class FriendListItem : UserControl {
    public ChatFriend friend { get; private set; }

    public FriendListItem() {
      InitializeComponent();

      if (Client.Session.Connected)
        Loaded += (src, e) => {
          Client.Session.ChatManager.Tick += (src2, e2) => Dispatcher.Invoke(Update);
          friend = (ChatFriend) DataContext;
          Update();
        };
    }

    public void Update() {
      if (friend.Status == null) return;

      long id = RiotChat.GetSummonerId(friend.User.Jid);
      string name;
      if (Client.Session.Settings.Nicknames.TryGetValue(id, out name)) {
        NameText.ToolTip = friend.User.Name;
      } else {
        name = friend.User.Name;
      }
      //TODO Nicknames

      NameText.Content = name;
      MsgText.Content = friend.Status.Message;
      if (string.IsNullOrWhiteSpace(friend.Status.Message)) {
        MsgText.Visibility = Visibility.Collapsed;
      } else {
        MsgText.Visibility = Visibility.Visible;
      }
      ProfileIconDto dto;
      if (DataDragon.IconData.Value.data.TryGetValue(friend.Status.ProfileIcon.ToString(), out dto)) {
        SummonerIcon.Source = DataDragon.GetProfileIconImage(dto).Load();
      }
      switch (friend.Status.Show) {
        case ShowType.chat: StatusText.Foreground = NameText.Foreground = App.ChatBrush; break;
        case ShowType.away: StatusText.Foreground = NameText.Foreground = App.AwayBrush; break;
        case ShowType.dnd: StatusText.Foreground = NameText.Foreground = App.BusyBrush; break;
      }

      TimeText.Visibility = ChampText.Visibility = Visibility.Collapsed;
      if (friend.CurrentGameDTO != null) {
        TimeText.Visibility = Visibility.Visible;
        if (friend.CurrentGameInfo == null) {
          long time = friend.Status.TimeStamp - Client.GetMilliseconds();
          TimeText.Content = "~" + TimeSpan.FromMilliseconds(time).ToString("m\\:ss");
        } else if (friend.CurrentGameInfo.gameStartTime == 0) {
          TimeText.Content = "Loading";
        } else {
          long time = friend.CurrentGameInfo.gameStartTime - Client.GetMilliseconds();
          TimeText.Content = TimeSpan.FromMilliseconds(time).ToString("m\\:ss");
        }
        StatusText.Content = QueueType.Values[friend.CurrentGameDTO.QueueTypeName].Value;
        if (!string.IsNullOrEmpty(friend.Status.Champion)) {
          ChampText.Visibility = Visibility.Visible;
          ChampText.Content = DataDragon.ChampData.Value.data[friend.Status.Champion].name;
        }
      } else StatusText.Content = friend.Status.GameStatus.Value;
      if (friend.Invite != null) {
        ChampText.Visibility = Visibility.Visible;
        ChampText.Content = "Invited you";
      }
    }

    private void Invite_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameInvitationService.Invite(friend.Cache.Data.Summoner.SummonerId);
    }

    private void ViewProfile_Click(object sender, RoutedEventArgs e) {
      Client.Session.QueueManager.ViewProfile(friend.Cache.Data.Summoner.Name);
    }

    private void DeclineButt_Click(object sender, RoutedEventArgs e) {
      if (friend.Invite == null) return;
      RiotServices.GameInvitationService.Decline(friend.Invite.InvitationId);
      friend.Invite = null;
      AcceptButt.BeginStoryboard(App.FadeOut);
      DeclineButt.BeginStoryboard(App.FadeOut);
    }

    private void AcceptButt_Click(object sender, RoutedEventArgs e) {
      if (friend.Invite == null) return;
      Client.Session.QueueManager.AcceptInvite(friend.Invite);
      friend.Invite = null;
      AcceptButt.BeginStoryboard(App.FadeOut);
      DeclineButt.BeginStoryboard(App.FadeOut);
    }

    private void Champ_MouseEnter(object sender, MouseEventArgs e) {
      if (friend?.Invite != null) {
        AcceptButt.BeginStoryboard(App.FadeIn);
        DeclineButt.BeginStoryboard(App.FadeIn);
      }
    }

    private void Champ_MouseLeave(object sender, MouseEventArgs e) {
      AcceptButt.BeginStoryboard(App.FadeOut);
      DeclineButt.BeginStoryboard(App.FadeOut);
    }
  }
}
