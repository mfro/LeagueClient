using System;
using RtmpSharp.IO;
using System.Collections.Generic;
using System.Collections;
using RtmpSharp.IO.AMF3;
using System.Text;
using System.Linq;
using System.Reflection;
using MFroehlich.Parsing.JSON;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.systemstate.ClientSystemStatesNotification")]
  public class ClientSystemStatesNotification : JSONSerializable, IExternalizable {
    public Boolean championTradeThroughLCDS { get; set; }

    public Boolean practiceGameEnabled { get; set; }

    public Boolean advancedTutorialEnabled { get; set; }

    public Int32[] practiceGameTypeConfigIdList { get; set; }

    public Int32 minNumPlayersForPracticeGame { get; set; }

    public Int32[] PracticeGameTypeConfigIdList { get; set; }

    public Int32[] freeToPlayChampionIdList { get; set; }

    public object[] inactiveChampionIdList { get; set; }

    public Int32[] inactiveSpellIdList { get; set; }

    public Int32[] inactiveTutorialSpellIdList { get; set; }

    public Int32[] inactiveClassicSpellIdList { get; set; }

    public Int32[] inactiveOdinSpellIdList { get; set; }

    public Int32[] inactiveAramSpellIdList { get; set; }

    public Int32[] enabledQueueIdsList { get; set; }

    public Int32[] unobtainableChampionSkinIDList { get; set; }

    public Int32[] freeToPlayChampionForNewPlayersIdList { get; set; }

    public Dictionary<String, Object> gameModeToInactiveSpellIds { get; set; }

    public Boolean archivedStatsEnabled { get; set; }

    public Dictionary<String, Object> queueThrottleDTO { get; set; }

    public Dictionary<String, Object>[] gameMapEnabledDTOList { get; set; }

    public Boolean storeCustomerEnabled { get; set; }

    public Boolean socialIntegrationEnabled { get; set; }

    public Boolean runeUniquePerSpellBook { get; set; }

    public Boolean tribunalEnabled { get; set; }

    public Boolean observerModeEnabled { get; set; }

    public Int32 currentSeason { get; set; }

    public Int32 freeToPlayChampionsForNewPlayersMaxLevel { get; set; }

    public Int32 spectatorSlotLimit { get; set; }

    public Int32 clientHeartBeatRateSeconds { get; set; }

    public String[] observableGameModes { get; set; }

    public String observableCustomGameModes { get; set; }

    public Boolean teamServiceEnabled { get; set; }

    public Boolean leagueServiceEnabled { get; set; }

    public Boolean modularGameModeEnabled { get; set; }

    public Double riotDataServiceDataSendProbability { get; set; }

    public Boolean displayPromoGamesPlayedEnabled { get; set; }

    public Boolean masteryPageOnServer { get; set; }

    public Int32 maxMasteryPagesOnServer { get; set; }

    public Boolean tournamentSendStatsEnabled { get; set; }

    public String replayServiceAddress { get; set; }

    public Boolean kudosEnabled { get; set; }

    public Boolean buddyNotesEnabled { get; set; }

    public Boolean localeSpecificChatRoomsEnabled { get; set; }

    public Dictionary<String, Object> replaySystemStates { get; set; }

    public Boolean sendFeedbackEventsEnabled { get; set; }

    public String[] knownGeographicGameServerRegions { get; set; }

    public Boolean leaguesDecayMessagingEnabled { get; set; }

    public string Json { get; set; }

    public void ReadExternal(IDataInput input) {
      Json = input.ReadUtf((int) input.ReadUInt32());

      var json = JSONParser.ParseObject(Json, 0);
      var states = JSONDeserializer.Deserialize<ClientSystemStatesNotification>(json);
      foreach (PropertyInfo prop in typeof(ClientSystemStatesNotification).GetProperties()) {
        prop.SetValue(this, prop.GetValue(states));
      }
      //foreach (KeyValuePair<string, object> keyPair in json) {
      //  var f = typeof(ClientSystemStatesNotification).GetProperty(keyPair.Key);
      //  if (f == null) continue;
      //  if (keyPair.Value is MFroehlich.Parsing.JSON.JSONArray) {
      //    var tempArrayList = keyPair.Value as MFroehlich.Parsing.JSON.JSONArray;
      //    if (tempArrayList.Count > 0) {
      //      var array = Array.CreateInstance(f.PropertyType.GetElementType(), tempArrayList.Count);
      //      for (int i = 0; i < tempArrayList.Count; i++) array.SetValue(tempArrayList[i], i);
      //      f.SetValue(this, array);
      //    } else
      //      f.SetValue(this, null);
      //  } else {
      //    f.SetValue(this, (dynamic) keyPair.Value);
      //  }
      //}
    }

    public ClientSystemStatesNotification() { }

    public void WriteExternal(IDataOutput output) {
      var bytes = Encoding.UTF8.GetBytes(Json);

      output.WriteInt32(bytes.Length);
      output.WriteBytes(bytes);
    }
  }
}