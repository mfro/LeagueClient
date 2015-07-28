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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChatConversation.xaml
  /// </summary>
  public partial class ChatConversation : UserControl {
    public delegate void ChatSendHandler (string user, string msg);
    public delegate void ChatWindowHandler (string user);

    public event ChatSendHandler MessageSent;
    public event ChatWindowHandler ChatOpened;
    public event ChatWindowHandler ChatClosed;

    public string UserName { get; private set; }
    public string User { get; private set; }
    public string History {
      get { return ChatHistory.Text; }
      set { ChatHistory.Text = value; }
    }
    public bool Unread {
      get { return unread; }
      set {
        if (value)
          (App.UnreadPulse).Begin(ChatOpenButt, true);
        else
          (App.UnreadPulse).Remove(ChatOpenButt);
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
          if (ChatOpened != null) ChatOpened(User);
          ChatSendBox.Focus();
        }
        open = value;
      }
    }

    private bool unread;
    private bool open;
    public ChatConversation(string name, string user) {
      InitializeComponent();
      this.UserName = name;
      this.User = user;
      this.ChatOpenButt.Content = name;
      this.GotFocus += OnFocus;
    }

    private void ChatOpenButt_Click(object sender, RoutedEventArgs e) {
      Open = !open;
    }

    private void ChatSendBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key != Key.Enter || ChatSendBox.Text.Length == 0) return;
      if (MessageSent != null)
        MessageSent(User, ChatSendBox.Text);
      ChatSendBox.Text = "";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
      if (ChatClosed != null) ChatClosed(User);
    }

    private void OnFocus(object sender, RoutedEventArgs e) {
      ChatSendBox.Focus();
    }
  }
}
