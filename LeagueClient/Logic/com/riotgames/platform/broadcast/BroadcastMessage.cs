﻿using MFroehlich.Parsing.JSON;

namespace LeagueClient.Logic.Riot.Platform {
  [JSONSerializable]
  public class BroadcastMessage {
    [JSONField("id")]
    public int Id { get; set; }
    [JSONField("active")]
    public bool Active { get; set; }
    [JSONField("content")]
    public string Content { get; set; }
    [JSONField("messageKey")]
    public string MessageKey { get; set; }
    [JSONField("severity")]
    public string Severity { get; set; }
  }
}