using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot;
using RiotClient.Chat;

namespace RiotClient.Lobbies {
  public class CustomLobby : Lobby {
    public override int QueueID {
      get { throw new NotImplementedException(); }
    }

    public CustomGame Game { get; }

    private CustomLobby(CustomGame game) {
      Game = game;
    }

    internal static CustomLobby Create(CustomGame game) {
      return new CustomLobby(game);
    }

    internal static CustomLobby Join(Invitation invite, CustomGame game) {
      var lobby = new CustomLobby(game);
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public override void EnterQueue() => new NotImplementedException();

    protected override void GotLobbyStatus(LobbyStatus status) {
      base.GotLobbyStatus(status);

      var left = new List<long>(Members.Keys);

      foreach (var raw in status.Members) {
        LobbyMember member;

        if (Members.TryGetValue(raw.SummonerId, out member)) {
          member.Update(raw);
          left.Remove(raw.SummonerId);
        }
      }

      foreach (var id in left) {
        OnMemberLeft(Members[id]);
        Members.Remove(id);
      }

      if (!loaded) {
        OnLoaded();
      }
    }

    public class CustomLobbyMember : LobbyMember {
      public CustomLobbyMember(Member raw) : base(raw) { }
    }
  }
}
