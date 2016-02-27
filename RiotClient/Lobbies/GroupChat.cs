using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class GroupChat : IDisposable {
    public event EventHandler<ChatMessageEventArgs> ChatMessage;
    public event EventHandler<ChatStatusEventArgs> ChatStatus;

    public Dictionary<string, Item> Users { get; } = new Dictionary<string, Item>();

    private Jid chatID;

    public GroupChat(Jid id, string pass = null) {
      chatID = id;

      Session.Current.ChatManager.PresenceRecieved += ChatManager_PresenceRecieved;
      Session.Current.ChatManager.MessageReceived += ChatManager_MessageReceived;
      Session.Current.ChatManager.JoinRoom(chatID, pass);
      Session.Current.ChatManager.SendPresence();
    }

    public void SendMessage(string message) {
      Session.Current.ChatManager.SendMessage(chatID, message, MessageType.groupchat);
    }

    public void Dispose() {
      Session.Current.ChatManager.PresenceRecieved -= ChatManager_PresenceRecieved;
      Session.Current.ChatManager.MessageReceived -= ChatManager_MessageReceived;
      Session.Current.ChatManager.LeaveRoom(chatID);
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!e.From.User.Equals(chatID.User) || e.Type != MessageType.groupchat) return;

      OnChatMessage(e.From.Resource, e.Body);
    }

    private void ChatManager_PresenceRecieved(object sender, Presence e) {
      if (e.Status == null && e.Type == PresenceType.available) {
        Session.Current.ChatManager.SendPresence();
        return;
      } else if (!e.From.User.Equals(chatID.User)) return;

      var user = e.MucUser.Item;
      if (e.Type == PresenceType.available) {
        if (!Users.ContainsKey(user.Jid.User)) {
          OnChatStatus(e.From.Resource, true);
          Users.Add(user.Jid.User, user);
        }
      } else {
        if (Users.ContainsKey(user.Jid.User))
          OnChatStatus(e.From.Resource, false);
        Users.Remove(user.Jid.User);
      }
    }

    private void OnChatMessage(string name, string body) {
      ChatMessage?.Invoke(this, new ChatMessageEventArgs(name, body));
    }

    private void OnChatStatus(string name, bool online) {
      ChatStatus?.Invoke(this, new ChatStatusEventArgs(name, online));
    }
  }

  public class ChatMessageEventArgs {
    public string Message { get; }
    public string From { get; }

    public ChatMessageEventArgs(string from, string message) {
      Message = message;
      From = from;
    }
  }

  public class ChatStatusEventArgs {
    public string From { get; }
    public bool IsOnline { get; }

    public ChatStatusEventArgs(string from, bool status) {
      From = from;
      IsOnline = status;
    }
  }
}
