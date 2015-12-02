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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChatConversation.xaml
  /// </summary>
  public partial class ChatConversation : UserControl {
    public event EventHandler ChatClosed;

    public ChatFriend friend { get; private set; }
    public bool Unread {
      get { return unread; }
      set {
        UnreadIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        unread = value;
      }
    }
    public bool Open {
      get { return open; }
      set {
        if (!value)
          ChatDisplayPanel.BeginStoryboard(App.FadeOut);
        else {
          if (Unread) Unread = false;
          ChatDisplayPanel.BeginStoryboard(App.FadeIn);
          ChatSendBox.Focus();
        }
        open = value;
      }
    }

    private bool unread;
    private bool open;

    public ChatConversation() {
      InitializeComponent();

      if (Client.Connected) {
        Unread = false;
        Loaded += (src, e) => {
          friend = (ChatFriend) DataContext;
          friend.HistoryUpdated += Friend_HistoryUpdated;
          NameLabel.Content = friend.User.Nickname;
          GotFocus += OnFocus;
        };
      }
    }

    private void ChatOpenButt_Click(object sender, RoutedEventArgs e) {
      Open = !Open;
      if (Open) ChatSendBox.MyFocus();
    }

    private void ChatSendBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key != Key.Enter || ChatSendBox.Text.Length == 0) return;
      friend.SendMessage(ChatSendBox.Text);
      ChatSendBox.Text = "";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
      ChatClosed?.Invoke(this, new EventArgs());
    }

    private void OnFocus(object sender, RoutedEventArgs e) {
      if (!Open) ChatSendBox.MyFocus();
    }

    private void Friend_HistoryUpdated(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        ChatHistory.Text = friend.History;
        if (!Open) Unread = true;
      });
    }
  }
}
