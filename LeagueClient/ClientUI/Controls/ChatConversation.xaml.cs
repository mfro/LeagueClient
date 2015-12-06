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
  public sealed partial class ChatConversation : UserControl, IDisposable {
    public event EventHandler ChatClosed;

    public ChatFriend friend { get; private set; }

    public bool Open {
      get { return open; }
      set {
        if (!value)
          ChatDisplayPanel.BeginStoryboard(App.FadeOut);
        else {
          UnreadIndicator.Visibility = Visibility.Collapsed;
          friend.Unread = false;
          ChatDisplayPanel.BeginStoryboard(App.FadeIn);
          ChatSendBox.Focus();
        }
        open = value;
      }
    }
    private bool open;

    public ChatConversation() {
      InitializeComponent();

      if (Client.Connected) {
        Loaded += (src, e) => {
          friend = (ChatFriend) DataContext;
          ChatHistory.Text = friend.History;
          if (!friend.Unread) Open = true;

          friend.HistoryUpdated += Friend_HistoryUpdated;
          NameLabel.Content = friend.User.Name;
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
      Dispose();
    }

    private void OnFocus(object sender, RoutedEventArgs e) {
      if (!Open) ChatSendBox.MyFocus();
    }

    private void Friend_HistoryUpdated(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        if (!Open) UnreadIndicator.Visibility = Visibility.Visible;
        else friend.Unread = false;
        ChatHistory.Text = friend.History;
      });
    }

    public void Dispose() {
      friend.HistoryUpdated -= Friend_HistoryUpdated;
    }
  }
}
