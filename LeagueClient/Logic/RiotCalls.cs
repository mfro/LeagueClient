using System;
using RtmpSharp.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using LeagueClient.Logic.Riot.Platform;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using RtmpSharp.Messaging;
using MFroehlich.Parsing.DynamicJSON;
using LeagueClient.Logic.Riot.Kudos;
using static LeagueClient.Logic.Strings;
using MFroehlich.League.DataDragon;
using LeagueClient.Logic.Cap;

namespace LeagueClient.Logic.Riot {
  public class RiotCalls {

    public delegate void OnInvocationErrorHandler(object sender, Exception error);
    public static event OnInvocationErrorHandler OnInvocationError;

    internal static Dictionary<string, Action<LcdsServiceProxyResponse>> Delegates { get; } = new Dictionary<string, Action<LcdsServiceProxyResponse>>();

    public static void AddHandler(Guid messageId, Action<LcdsServiceProxyResponse> del) {
      Delegates.Add(messageId.ToString(), del);
    }

    public static class LoginService {
      /// <summary>
      /// Login to Riot's servers.
      /// </summary>
      /// <param name="Credentials">The credentials for the user</param>
      /// <returns>Session information for the user</returns>
      public static Task<Session> Login(AuthenticationCredentials Credentials) {
        return InvokeAsync<Session>("loginService", "login", Credentials);
      }

      /// <summary>
      /// Heartbeat to send every 2 minutes.
      /// </summary>
      /// <param name="AccountId">The users id</param>
      /// <param name="SessionToken">The token for the user</param>
      /// <param name="HeartbeatCount">The current amount that heartbeat has been sent</param>
      /// <param name="CurrentTime">The current time in GMT-0700 in format ddd MMM d yyyy HH:mm:ss</param>
      public static Task<string> PerformLCDSHeartBeat(Int32 AccountId, String SessionToken, Int32 HeartbeatCount, String CurrentTime) {
        return InvokeAsync<string>("loginService", "performLCDSHeartBeat", AccountId, SessionToken, HeartbeatCount, CurrentTime);
      }

      /// <summary>
      /// Gets the store url with token information for the current user.
      /// </summary>
      /// <returns>Returns the store URL</returns>
      public static Task<String> GetStoreUrl() {
        return InvokeAsync<String>("loginService", "getStoreUrl");
      }

      /// <summary>
      /// Log out of Riot's servers
      /// </summary>
      /// <returns></returns>
      public static Task Logout() {
        return InvokeAsync<object>("loginService", "logout");
      }
    }

    public static class AccountService {
      /// <summary>
      /// Gets the state for the current account
      /// </summary>
      /// <returns>Return the accounts state</returns>
      public static Task<String> GetAccountState() {
        return InvokeAsync<String>("accountService", "getAccountStateForCurrentSession");
      }
    }

    public static class ClientFacadeService {
      /// <summary>
      /// Gets the login packet for the user with all the information for the user.
      /// </summary>
      /// <returns>Returns the login data packet</returns>
      public static Task<LoginDataPacket> GetLoginDataPacketForUser() {
        return InvokeAsync<LoginDataPacket>("clientFacadeService", "getLoginDataPacketForUser");
      }

      /// <summary>
      /// Call kudos (get information or send a kudos).
      /// </summary>
      /// <param name="JSONInformation"></param>
      /// <returns>Json Data about kudos</returns>
      public static Task<LcdsResponseString> CallKudos(String JSONInformation) {
        return InvokeAsync<LcdsResponseString>("clientFacadeService", "callKudos", JSONInformation);
      }

      //TODO Finish riot calls
    }

    public static class MatchmakerService {
      /// <summary>
      /// Attemps to leave a queue
      /// </summary>
      /// <param name="SummonerId">The users summoner id</param>
      /// <returns>If successfully cancelled returns true, otherwise champion select about to start</returns>
      public static Task<Boolean> CancelFromQueueIfPossible(Double SummonerId) {
        return InvokeAsync<Boolean>("matchmakerService", "cancelFromQueueIfPossible", SummonerId);
      }

      /// <summary>
      /// Gets the queue information for a selected queue
      /// </summary>
      /// <param name="QueueId">The queue id</param>
      /// <returns>Returns the queue information</returns>
      public static Task<QueueInfo> GetQueueInformation(Double QueueId) {
        return InvokeAsync<QueueInfo>("matchmakerService", "getQueueInfo", QueueId);
      }

      public static Task<AsObject> PurgeFromQueues() {
        return InvokeAsync<AsObject>("matchmakerService", "purgeFromQueues");
      }

      /// <summary>
      /// Attaches to a queue
      /// </summary>
      /// <param name="MatchMakerParams">The parameters for the queue</param>
      /// <returns>Returns a notification to tell you if it was successful</returns>
      public static Task<SearchingForMatchNotification> AttachToQueue(MatchMakerParams MatchMakerParams) {
        return InvokeAsync<SearchingForMatchNotification>("matchmakerService", "attachToQueue", MatchMakerParams);
      }

      /// <summary>
      /// Attaches a premade team to a queue
      /// </summary>
      /// <param name="MatchMakerParams">The parameters for the queue</param>
      /// <returns>Returns a notification to tell you if it was successful</returns>
      [Obsolete]
      public static Task<SearchingForMatchNotification> AttachTeamToQueue(MatchMakerParams matchMakerParams) {
        return InvokeAsync<SearchingForMatchNotification>("matchmakerService", "attachTeamToQueue", matchMakerParams);
      }

      /// <summary>
      /// Get the queues that are currently enabled.
      /// </summary>
      /// <returns>Returns an array of queues that are enabled</returns>
      public static Task<GameQueueConfig[]> GetAvailableQueues() {
        return InvokeAsync<GameQueueConfig[]>("matchmakerService", "getAvailableQueues");
      }

      /// <summary>
      /// Accepts an invite to a matchmaking game
      /// </summary>
      /// <param name="InviteId">The invite id</param>
      [Obsolete]
      public static Task<Object> AcceptInviteForMatchmakingGame(String InviteId) {
        return InvokeAsync<Object>("matchmakerService", "acceptInviteForMatchmakingGame", InviteId);
      }
    }

    public static class InventoryService {
      /// <summary>
      /// Get the current IP & EXP Boosts for the user.
      /// </summary>
      /// <returns>Returns the active boosts for the user</returns>
      public static Task<SummonerActiveBoostsDTO> GetSumonerActiveBoosts() {
        return InvokeAsync<SummonerActiveBoostsDTO>("inventoryService", "getSumonerActiveBoosts");
      }

      /// <summary>
      /// Get the current champions for the user.
      /// </summary>
      /// <returns>Returns an array of champions</returns>
      public static Task<ChampionDTO[]> GetAvailableChampions() {
        return InvokeAsync<ChampionDTO[]>("inventoryService", "getAvailableChampions");
      }
    }

    public static class SummonerRuneService {
      /// <summary>
      /// Get the runes the user owns.
      /// </summary>
      /// <param name="SummonerId">The summoner ID for the user</param>
      /// <returns>Returns the inventory for the user</returns>
      public static Task<SummonerRuneInventory> GetSummonerRuneInventory(Double SummonerId) {
        return InvokeAsync<SummonerRuneInventory>("summonerRuneService", "getSummonerRunes", SummonerId);
      }

      /// <summary>
      /// Get the current Mastery Book for the user.
      /// </summary>
      /// <param name="SummonerId">The summoner ID for the user</param>
      /// <returns>Returns the mastery books for the user</returns>
      public static Task<MasteryBookDTO> GetMasteryBook(Double SummonerId) {
        return InvokeAsync<MasteryBookDTO>("summonerRuneService", "getSummonerRuneInventory", SummonerId);
      }
    }

    public static class SpellBookService {
      /// <summary>
      /// Gets the runes for a user
      /// </summary>
      /// <param name="SummonerId">The summoner ID for the user</param>
      /// <returns>Returns the rune pages for a user</returns>
      public static Task<Object> GetSpellBook(Double SummonerId) {
        return InvokeAsync<Object>("spellBookService", "getSpellBook", SummonerId);
      }

      /// <summary>
      /// Selects a rune page for use
      /// </summary>
      /// <param name="SpellbookPage">The spellbook page the player wants to use</param>
      /// <returns>The selected spellbook page</returns>
      public static Task<Object> SelectDefaultSpellBookPage(SpellBookPageDTO SpellbookPage) {
        return InvokeAsync<object>("spellBookService", "selectDefaultSpellBookPage", SpellbookPage);
      }

      /// <summary>
      /// Saves the players spellbook
      /// </summary>
      /// <param name="Spellbook">The players SpellBookDTO</param>
      public static Task<Object> SaveSpellBook(SpellBookDTO Spellbook) {
        return InvokeAsync<Object>("spellBookService", "saveSpellBook", Spellbook);
      }
    }

    public static class LeaguesService {
      /// <summary>
      /// Gets the league positions for the user
      /// </summary>
      /// <returns>Returns the league positions for a user</returns>
      public static Task<SummonerLeagueItemsDTO> GetMyLeaguePositions() {
        return InvokeAsync<SummonerLeagueItemsDTO>("leaguesServiceProxy", "getMyLeaguePositions");
      }

      /// <summary>
      /// Gets the top 50 players for a queue type
      /// </summary>
      /// <param name="queueType">Queue type</param>
      /// <returns>Returns the top 50 players league info</returns>
      public static Task<SummonerLeaguesDTO> GetChallengerLeague(String queueType) {
        return InvokeAsync<SummonerLeaguesDTO>("leaguesServiceProxy", "getChallengerLeague", queueType);
      }

      /// <summary>
      /// Gets the current leagues for a user's tier (e.g Gold)
      /// </summary>
      /// <returns>Returns the leagues for a user</returns>
      public static Task<SummonerLeaguesDTO> GetAllMyLeagues() {
        return InvokeAsync<SummonerLeaguesDTO>("leaguesServiceProxy", "getAllMyLeagues");
      }

      /// <summary>
      /// Gets the league for a team
      /// </summary>
      /// <param name="TeamName">The team name</param>
      /// <returns>Returns the league information for a team</returns>
      public static Task<SummonerLeaguesDTO> GetLeaguesForTeam(String TeamName) {
        return InvokeAsync<SummonerLeaguesDTO>("leaguesServiceProxy", "getLeaguesForTeam", TeamName);
      }

      /// <summary>
      /// Get the leagues for a player
      /// </summary>
      /// <param name="SummonerId">The summoner id of the player</param>
      /// <returns>Returns the league information for a team</returns>
      public static Task<SummonerLeaguesDTO> GetAllLeaguesForPlayer(Double SummonerId) {
        return InvokeAsync<SummonerLeaguesDTO>("leaguesServiceProxy", "getAllLeaguesForPlayer", SummonerId);
      }
    }

    public static class SummonerTeamService {
      /// <summary>
      /// 
      /// </summary>
      public static Task<Player> CreatePlayer() {
        return InvokeAsync<Player>("summonerTeamService", "createPlayer");
      }

      /// <summary>
      /// Find a team by the TeamId
      /// </summary>
      /// <param name="TeamId">The team Id</param>
      /// <returns>Returns the information for a team</returns>
      public static Task<Team.TeamDTO> FindTeamById(Team.TeamId TeamId) {
        return InvokeAsync<Team.TeamDTO>("summonerTeamService", "findTeamById", TeamId);
      }

      /// <summary>
      /// Find a team by name
      /// </summary>
      /// <param name="TeamName">The team name</param>
      /// <returns>Returns the information for a team</returns>
      public static Task<Team.TeamDTO> FindTeamByName(String TeamName) {
        return InvokeAsync<Team.TeamDTO>("summonerTeamService", "findTeamByName", TeamName);
      }

      /// <summary>
      /// Disbands a team
      /// </summary>
      /// <param name="TeamId">The team Id</param>
      public static Task<Object> DisbandTeam(Team.TeamId TeamId) {
        return InvokeAsync<Object>("summonerTeamService", "disbandTeam", TeamId);
      }

      /// <summary>
      /// Checks if a name is available 
      /// </summary>
      /// <param name="TeamName">The name that you want to validate</param>
      /// <returns>Returns a boolean as the result</returns>
      public static Task<Boolean> IsTeamNameValidAndAvailable(String TeamName) {
        return InvokeAsync<Boolean>("summonerTeamService", "isNameValidAndAvailable", TeamName);
      }

      /// <summary>
      /// Checks if a tag is available 
      /// </summary>
      /// <param name="TeamName">The tag that you want to validate</param>
      /// <returns>Returns a boolean as the result</returns>
      public static Task<Boolean> IsTeamTagValidAndAvailable(String TagName) {
        return InvokeAsync<Boolean>("summonerTeamService", "isTagValidAndAvailable", TagName);
      }

      /// <summary>
      /// Creates a ranked team if the name and tag is valid
      /// </summary>
      /// <param name="TeamName">The team name</param>
      /// <param name="TagName">The tag name</param>
      /// <returns>Returns the information for a team</returns>
      public static Task<Team.TeamDTO> CreateTeam(String TeamName, String TagName) {
        return InvokeAsync<Team.TeamDTO>("summonerTeamService", "createTeam", TeamName, TagName);
      }

      /// <summary>
      /// Invites a player to a ranked team
      /// </summary>
      /// <param name="SummonerId">The summoner id of the player you want to invite</param>
      /// <param name="TeamId">The team id</param>
      /// <returns>Returns the information for a team</returns>
      public static Task<Team.TeamDTO> TeamInvitePlayer(Double SummonerId, Team.TeamId TeamId) {
        return InvokeAsync<Team.TeamDTO>("summonerTeamService", "invitePlayer", SummonerId, TeamId);
      }

      /// <summary>
      /// Kicks a player from a ranked team
      /// </summary>
      /// <param name="SummonerId">The summoner id of the player you want to kick</param>
      /// <param name="TeamId">The team id</param>
      /// <returns>Returns the information for a team</returns>
      public static Task<Team.TeamDTO> KickPlayer(Double SummonerId, Team.TeamId TeamId) {
        return InvokeAsync<Team.TeamDTO>("summonerTeamService", "kickPlayer", SummonerId, TeamId);
      }

      /// <summary>
      /// Finds a player by Summoner Id
      /// </summary>
      /// <param name="SummonerId">The summoner id</param>
      /// <returns>Returns the information for a player</returns>
      public static Task<Player> FindPlayer(Double SummonerId) {
        return InvokeAsync<Player>("summonerTeamService", "findPlayer", SummonerId);
      }
    }
    
    public static class SummonerService {
      /// <summary>
      /// Gets summoner data by account id
      /// </summary>
      /// <param name="AccountId">The account id</param>
      /// <returns>Returns all the summoner data for an account</returns>
      public static Task<AllSummonerData> GetallSummonerDataByAccount(Double AccountId) {
        return InvokeAsync<AllSummonerData>("summonerService", "getallSummonerDataByAccount");
      }

      /// <summary>
      /// Gets summoner by name
      /// </summary>
      /// <param name="SummonerName">The name of the summoner</param>
      /// <returns>Returns the summoner</returns>
      public static Task<PublicSummoner> GetSummonerByName(String SummonerName) {
        return InvokeAsync<PublicSummoner>("summonerService", "getSummonerByName", SummonerName);
      }

      /// <summary>
      /// Gets the public summoner data by account id
      /// </summary>
      /// <param name="AccountId">The account id</param>
      /// <returns>Returns all the public summoner data for an account</returns>
      public static Task<AllPublicSummonerDataDTO> GetAllPublicSummonerDataByAccount(Double AccountId) {
        return InvokeAsync<AllPublicSummonerDataDTO>("summonerService", "getAllPublicSummonerDataByAccount", AccountId);
      }

      /// <summary>
      /// Gets the summoner internal name of a summoner
      /// </summary>
      /// <param name="SummonerName">The summoner name</param>
      /// <returns>Returns a summoners internal name</returns>
      public static Task<String> GetSummonerInternalNameByName(String SummonerName) {
        return InvokeAsync<String>("summonerService", "getSummonerInternalNameByName", SummonerName);
      }

      /// <summary>
      /// Updates the profile icon for the user
      /// </summary>
      /// <param name="IconId">The icon id</param>
      public static Task<Object> UpdateProfileIconId(Int32 IconId) {
        return InvokeAsync<Object>("summonerService", "updateProfileIconId", IconId);
      }

      /// <summary>
      /// Get the summoner names for an array of Summoner IDs.
      /// </summary>
      /// <param name="SummonerIds">Array of Summoner IDs</param>
      /// <returns>Returns an array of Summoner Names</returns>
      public static Task<String[]> GetSummonerNames(Double[] SummonerIds) {
        return InvokeAsync<String[]>("summonerService", "getSummonerNames", SummonerIds);
      }

      /// <summary>
      /// Sends a players display name when logging in.
      /// </summary>
      /// <param name="PlayerName">Display name for the summoner</param>
      /// <returns></returns>
      public static Task<AllSummonerData> CreateDefaultSummoner(String PlayerName) {
        return InvokeAsync<AllSummonerData>("summonerService", "createDefaultSummoner", PlayerName);
      }
    }

    public static class PlayerStatsService {
      /// <summary>
      /// Sends the skill of the player to the server when initially logging in to seed MMR.
      /// </summary>
      /// <param name="PlayerSkill">The skill of the player</param>
      /// <returns></returns>
      public static Task<Object> ProcessELOQuestionaire(PlayerSkill PlayerSkill) {
        return InvokeAsync<Object>("playerStatsService", "processEloQuestionaire", PlayerSkill.ToString());
      }

      /// <summary>
      /// Gets the players overall stats
      /// </summary>
      /// <param name="AccountId">The account id</param>
      /// <param name="Season">The season you want to retrieve stats from</param>
      /// <returns>Returns the player stats for a season</returns>
      public static Task<PlayerLifetimeStats> RetrievePlayerStatsByAccountId(Double AccountId, String Season) {
        return InvokeAsync<PlayerLifetimeStats>("playerStatsService", "retrievePlayerStatsByAccountId", AccountId, Season);
      }

      /// <summary>
      /// Gets the top 3 played champions for a player
      /// </summary>
      /// <param name="AccountId">The account id</param>
      /// <param name="GameMode">The game mode</param>
      /// <returns>Returns an array of the top 3 champions</returns>
      public static Task<ChampionStatInfo[]> RetrieveTopPlayedChampions(Double AccountId, String GameMode) {
        return InvokeAsync<ChampionStatInfo[]>("playerStatsService", "retrieveTopPlayedChampions", AccountId, GameMode);
      }

      /// <summary>
      /// Gets the aggregated stats of a players ranked games
      /// </summary>
      /// <param name="SummonerId">The summoner id of a player</param>
      /// <param name="GameMode">The game mode requested</param>
      /// <param name="Season">The season you want to retrieve stats from</param>
      /// <returns>Returns the aggregated stats requested</returns>
      public static Task<AggregatedStats> GetAggregatedStats(Double SummonerId, String GameMode, String Season) {
        return InvokeAsync<AggregatedStats>("playerStatsService", "getAggregatedStats", SummonerId, GameMode, Season);
      }

      /// <summary>
      /// Gets the top 10 recent games for a player
      /// </summary>
      /// <param name="AccountId">The account id of a player</param>
      /// <returns>Returns the recent games for a player</returns>
      public static Task<RecentGames> GetRecentGames(Double AccountId) {
        return InvokeAsync<RecentGames>("playerStatsService", "getRecentGames", AccountId);
      }

      /// <summary>
      /// Gets the aggregated stats for a team for all game modes
      /// </summary>
      /// <param name="TeamId">The team id</param>
      /// <returns>Returns an array </returns>
      public static Task<TeamAggregatedStatsDTO[]> GetTeamAggregatedStats(Team.TeamId TeamId) {
        return InvokeAsync<TeamAggregatedStatsDTO[]>("playerStatsService", "getTeamAggregatedStats", TeamId);
      }

      /// <summary>
      /// Gets the end of game stats for a team for any game
      /// </summary>
      /// <param name="TeamId">The team id</param>
      /// <param name="GameId">The game id</param>
      /// <returns>Returns the end of game stats for a game</returns>
      public static Task<EndOfGameStats> GetTeamEndOfGameStats(Team.TeamId TeamId, Double GameId) {
        return InvokeAsync<EndOfGameStats>("playerStatsService", "getTeamEndOfGameStats", TeamId, GameId);
      }
    }

    public static class RerollService {
      /// <summary>
      /// Gets the player reroll balance
      /// </summary>
      /// <returns>Returns the reroll balance for the player</returns>
      public static Task<PointSummary> GetPointsBalance() {
        return InvokeAsync<PointSummary>("lcdsRerollService", "getPointsBalance");
      }

      /// <summary>
      /// Attempts to reroll the champion. Only works in AllRandomPickStrategy
      /// </summary>
      /// <returns>Returns the amount of rolls left for the player</returns>
      public static Task<RollResult> Roll() {
        return InvokeAsync<RollResult>("lcdsRerollService", "roll");
      }
    }

    public static class GameService {
      /// <summary>
      /// Gets all the practice games
      /// </summary>
      /// <returns>Returns an array of practice games</returns>
      public static Task<PracticeGameSearchResult[]> ListAllPracticeGames() {
        return InvokeAsync<PracticeGameSearchResult[]>("gameService", "listAllPracticeGames");
      }

      /// <summary>
      /// Joins a game
      /// </summary>
      /// <param name="GameId">The game id the user wants to join</param>
      public static Task<Object> JoinGame(Double GameId) {
        return InvokeAsync<Object>("gameService", "joinGame", GameId, null);
      }

      /// <summary>
      /// Joins a private game
      /// </summary>
      /// <param name="GameId">The game id the user wants to join</param>
      /// <param name="Password">The password of the game</param>
      /// <returns></returns>
      public static Task<Object> JoinGame(Double GameId, String Password) {
        return InvokeAsync<Object>("gameService", "joinGame", GameId, Password);
      }

      public static Task<Object> ObserveGame(Double GameId) {
        return InvokeAsync<Object>("gameService", "observeGame", GameId, null);
      }

      public static Task<Object> ObserveGame(Double GameId, String Password) {
        return InvokeAsync<Object>("gameService", "observeGame", GameId, Password);
      }

      /// <summary>
      /// Switches the teams in a custom game
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <returns>Returns true if successful</returns>
      public static Task<Boolean> SwitchTeams(Double GameId) {
        return InvokeAsync<Boolean>("gameService", "switchTeams", GameId);
      }

      /// <summary>
      /// Switches from a player to spectator
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <returns>Returns true if successful</returns>
      public static Task<Boolean> SwitchPlayerToObserver(Double GameId) {
        return InvokeAsync<Boolean>("gameService", "switchPlayerToObserver", GameId);
      }

      /// <summary>
      /// Switches from a spectator to player
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <returns>Returns true if successful</returns>
      public static Task<Boolean> SwitchObserverToPlayer(Double GameId, Int32 Team) {
        return InvokeAsync<Boolean>("gameService", "switchObserverToPlayer", GameId, Team);
      }

      /// <summary>
      /// Quits from the current game
      /// </summary>
      public static Task<Object> QuitGame() {
        return InvokeAsync<Object>("gameService", "quitGame");
      }

      /// <summary>
      /// Creates a practice game.
      /// </summary>
      /// <param name="Config">The configuration for the practice game</param>
      /// <returns>Returns a GameDTO if successfully created, otherwise null</returns>
      public static Task<GameDTO> CreatePracticeGame(PracticeGameConfig Config) {
        return InvokeAsync<GameDTO>("gameService", "createPracticeGame", Config);
      }

      /// <summary>
      /// Creates a practice game.
      /// </summary>
      /// <param name="Config">The configuration for the practice game</param>
      /// <returns>Returns a GameDTO if successfully created, otherwise null</returns>
      public static Task<GameDTO> CreateTutorialGame(int queueId) {
        return InvokeAsync<GameDTO>("gameService", "createTutorialGame", queueId);
      }

      /// <summary>
      /// Starts champion selection for a custom game
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <param name="OptomisticLock">The optomistic lock (provided by GameDTO)</param>
      /// <returns>Returns a StartChampSelectDTO</returns>
      public static Task<StartChampSelectDTO> StartChampionSelection(Double GameId, Double OptomisticLock) {
        return InvokeAsync<StartChampSelectDTO>("gameService", "startChampionSelection", GameId, OptomisticLock);
      }

      /// <summary>
      /// Send a message to the server
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <param name="Argument">The argument to be passed</param>
      public static Task<Object> SetClientReceivedGameMessage(Double GameId, String Argument) {
        return InvokeAsync<Object>("gameService", "setClientReceivedGameMessage", GameId, Argument);
      }

      /// <summary>
      /// Gets the latest GameDTO for a game
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <param name="GameState">The current game state</param>
      /// <param name="PickTurn">The current pick turn</param>
      /// <returns>Returns the latest GameDTO</returns>
      public static Task<GameDTO> GetLatestGameTimerState(Double GameId, String GameState, Int32 PickTurn) {
        return InvokeAsync<GameDTO>("gameService", "getLatestGameTimerState", GameId, GameState, PickTurn);
      }

      /// <summary>
      /// Selects the spells for a player for the current game
      /// </summary>
      /// <param name="SpellOneId">The spell id for the first spell</param>
      /// <param name="SpellTwoId">The spell id for the second spell</param>
      public static Task<Object> SelectSpells(Int32 SpellOneId, Int32 SpellTwoId) {
        return InvokeAsync<Object>("gameService", "selectSpells", SpellOneId, SpellTwoId);
      }
      /// <summary>
      /// Selects a champion for use
      /// </summary>
      /// <param name="ChampionId">The selected champion id</param>
      public static Task<Object> SelectChampion(Int32 ChampionId) {
        return InvokeAsync<Object>("gameService", "selectChampion", ChampionId);
      }

      /// <summary>
      /// Selects a champion skin for a champion
      /// </summary>
      /// <param name="ChampionId">The selected champion id</param>
      /// <param name="SkinId">The selected champion skin</param>
      public static Task<Object> SelectChampionSkin(Int32 ChampionId, Int32 SkinId) {
        return InvokeAsync<Object>("gameService", "selectChampionSkin", ChampionId, SkinId);
      }

      /// <summary>
      /// Lock in your champion selection
      /// </summary>
      public static Task<Object> ChampionSelectCompleted() {
        return InvokeAsync<Object>("gameService", "championSelectCompleted");
      }

      /// <summary>
      /// Gets the spectator game info for a summoner
      /// </summary>
      /// <param name="SummonerName">The summoner name</param>
      /// <returns>Returns the game info</returns>
      public static Task<PlatformGameLifecycleDTO> RetrieveInProgressSpectatorGameInfo(String SummonerName) {
        return InvokeAsync<PlatformGameLifecycleDTO>("gameService", "retrieveInProgressSpectatorGameInfo", SummonerName);
      }

      /// <summary>
      /// Accepts a popped queue
      /// </summary>
      /// <param name="AcceptGame">Accept or decline the queue</param>
      public static Task<Object> AcceptPoppedGame(Boolean AcceptGame) {
        return InvokeAsync<Object>("gameService", "acceptPoppedGame", AcceptGame);
      }

      /// <summary>
      /// Bans a user from a custom game
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <param name="AccountId">The account id of the player</param>
      public static Task<Object> BanUserFromGame(Double GameId, Double AccountId) {
        return InvokeAsync<Object>("gameService", "banUserFromGame", GameId, AccountId);
      }

      /// <summary>
      /// Bans a user from a custom game
      /// </summary>
      /// <param name="GameId">The game id</param>
      /// <param name="AccountId">The account id of the player</param>
      public static Task<Object> BanObserverFromGame(Double GameId, Double AccountId) {
        return InvokeAsync<Object>("gameService", "banObserverFromGame", GameId, AccountId);
      }

      /// <summary>
      /// Bans a champion from the game (must be during PRE_CHAMP_SELECT and the users PickTurn)
      /// </summary>
      /// <param name="ChampionId">The champion id</param>
      public static Task<Object> BanChampion(Int32 ChampionId) {
        return InvokeAsync<Object>("gameService", "banChampion", ChampionId);
      }

      /// <summary>
      /// Gets the champions from the other team to ban
      /// </summary>
      /// <returns>Returns an array of champions to ban</returns>
      public static Task<ChampionBanInfoDTO[]> GetChampionsForBan() {
        return InvokeAsync<ChampionBanInfoDTO[]>("gameService", "getChampionsForBan");
      }
    }

    public static class MasteryBookService {
      /// <summary>
      /// Saves the mastery book
      /// </summary>
      /// <param name="MasteryBookPage">The mastery book information</param>
      /// <returns>Returns the mastery book</returns>
      public static Task<MasteryBookDTO> SaveMasteryBook(MasteryBookDTO MasteryBookPage) {
        return InvokeAsync<MasteryBookDTO>("masteryBookService", "saveMasteryBook", MasteryBookPage);
      }
      /// <summary>
      /// Saves the mastery book
      /// </summary>
      /// <param name="MasteryBookPage">The mastery book information</param>
      /// <returns>Returns the mastery book</returns>
      public static Task<MasteryBookDTO> GetMasteryBook(double summonerId) {
        return InvokeAsync<MasteryBookDTO>("masteryBookService", "getMasteryBook", summonerId);
      }

      /// <summary>
      /// Selects the default mastery book
      /// </summary>
      /// <param name="page">The mastery book to select</param>
      /// <returns></returns>
      public static Task<Object> SelectDefaultMasteryBookPage(MasteryBookPageDTO page) {
        return InvokeAsync<Object>("masteryBookService", "selectDefaultMasteryBookPage", page);
      }
    }

    public static class SummonerIconService {
      /// <summary>
      /// Gets the summoner icons for a user
      /// </summary>
      /// <param name="SummonerId">The summoner id</param>
      /// <returns>Returns the summoner icons</returns>
      public static Task<SummonerIconInventoryDTO> GetSummonerIconInventory(Double SummonerId) {
        return InvokeAsync<SummonerIconInventoryDTO>("summonerIconService", "getSummonerIconInventory", SummonerId);
      }
    }

    public static class ChampionTradeService {
      /// <summary>
      /// Gets the allowed traders in the current game
      /// </summary>
      /// <returns>Returns a list of traders</returns>
      public static Task<AsObject> GetPotentialTraders() {
        return InvokeAsync<AsObject>("lcdsChampionTradeService", "getPotentialTraders");
      }

      /// <summary>
      /// Attempts to trade with a player
      /// </summary>
      /// <param name="SummonerInternalName">The internal name of a summoner</param>
      /// <param name="ChampionId">The champion id requested</param>
      public static Task<Object> AttemptTrade(String SummonerInternalName, Int32 ChampionId) {
        return InvokeAsync<Object>("lcdsChampionTradeService", "attemptTrade", SummonerInternalName, ChampionId, false);
      }

      /// <summary>
      /// Decline the current trade
      /// </summary>
      public static Task<Object> DeclineTrade() {
        return InvokeAsync<Object>("lcdsChampionTradeService", "dismissTrade");
      }

      /// <summary>
      /// Accepts the current trade
      /// </summary>
      /// <param name="SummonerInternalName">The internal name of a summoner</param>
      /// <param name="ChampionId">The champion id requested</param>
      public static Task<Object> AcceptTrade(String SummonerInternalName, Int32 ChampionId) {
        return InvokeAsync<Object>("lcdsChampionTradeService", "attemptTrade", SummonerInternalName, ChampionId, true);
      }
    }

    public static class LcdsService {
      /// <summary>
      /// Creates a team builder lobby
      /// </summary>
      /// <param name="QueueId">The queue ID for the lobby</param>
      /// <param name="UUID">The generated UUID of the lobby</param>
      /// <returns></returns>
      public static Task<LobbyStatus> CreateGroupFinderLobby(Int32 QueueId, String UUID) {
        return InvokeAsync<LobbyStatus>("lcdsServiceProxy", "createGroupFinderLobby", QueueId, UUID);
      }

      /// <summary>
      /// Sends a call to the LCDS Service Proxy
      /// </summary>
      /// <param name="UUID">The generated UUID of the service call</param>
      /// <param name="GameMode">The game mode (usually "cap")</param>
      /// <param name="ProcedureCall">The procedure to call</param>
      /// <param name="Parameters">The parameters to pass in JSON encoded format</param>
      /// <returns></returns>
      public static Task CallLCDS(String UUID, String GameMode, String ProcedureCall, String Parameters) {
        return InvokeAsync<Object>("lcdsServiceProxy", "call", UUID, GameMode, ProcedureCall, Parameters);
      }
    }

    public static class CapService {

      private static Guid Invoke(string method, JSONObject args = null) {
        var str = (args == null) ? "{}" : JSON.Stringify(args);
        var id = Guid.NewGuid();
        LcdsService.CallLCDS(id.ToString(), "cap", method, str);
        return id;
      }

      public static Guid AbandonLeaverBusterQueue(string accessTokenStr) {
        var json = new JSONObject();
        json["accessTokenStr"] = accessTokenStr;
        return Invoke("abandonLeaverBusterLowPriorityQueueV1", json);
      }

      public static Guid AcceptCandidate(double slotId) {
        var json = new JSONObject();
        json["slotId"] = slotId;
        return Invoke("acceptCandidateV2", json);
      }

      public static Guid CreateGroup() {
        var json = new JSONObject();
        json["queueId"] = 61;
        return Invoke("createGroupV3", json);
      }

      public static Guid CreateSoloQuery(CapPlayer player, string accessTokenStr = null) {
        var json = new JSONObject();
        json["championId"] = player.Champion.key;
        json["role"] = player.Role.Key;
        json["position"] = player.Position.Key;
        json["spell1Id"] = player.Spell1.key;
        json["spell2Id"] = player.Spell2.key;
        json["queueId"] = 61;
        if (accessTokenStr != null)
          json["accessTokenStr"] = accessTokenStr;
        return Invoke("createSoloQueryV5", json);
      }

      public static Guid DeclineCandidate(int slotId) {
        var json = new JSONObject();
        json["slotId"] = slotId;
        return Invoke("declineCandidateV2", json);
      }

      public static Guid IndicateGroupAcceptanceAsCandidate(int slotId, bool accept, string groupId) {
        var json = new JSONObject();
        json["slotId"] = slotId;
        json["acceptance"] = accept;
        json["groupId"] = groupId;
        return Invoke("indicateGroupAcceptanceAsCandidateV1", json);
      }

      public static Guid IndicateReadyness(bool ready) {
        var json = new JSONObject();
        json["ready"] = ready;
        return Invoke("indicateReadinessV1", json);
      }

      public static Guid JoinGroupAsInvitee(string groupId) {
        var json = new JSONObject();
        json["groupId"] = groupId;
        return Invoke("joinGroupAsInviteeV1", json);
      }

      public static Guid KickPlayer(int slotId) {
        var json = new JSONObject();
        json["slotId"] = slotId;
        return Invoke("kickPlayerV1", json);
      }

      public static Guid Quit() {
        return Invoke("quitV2");
      }

      public static Guid RetrieveWaitTime(int champId, Role role, Position pos) {
        var json = new JSONObject();
        json["championId"] = champId;
        json["role"] = role.Key;
        json["position"] = pos.Key;
        json["queuId"] = 61;
        return Invoke("retrieveEstimatedWaitTimeV2", json);
      }

      public static Guid RetrieveFeatureToggles() {
        return Invoke("retrieveFeatureToggles");
      }

      public static Guid RetrieveInfo() {
        var json = new JSONObject();
        json["queueId"] = 61;
        return Invoke("retrieveInfoV2", json);
      }

      public static Guid SearchForAnotherGroup() {
        return Invoke("searchForAnotherGroupV2");
      }

      public static Guid SearchForCandidates(string accessTokenStr = null) {
        var json = new JSONObject();
        if (accessTokenStr != null)
          json["accessTokenStr"] = accessTokenStr;
        return Invoke("startGroupBuildingPhaseV2", json);
      }

      public static Guid SelectAdvertisedPosition(Position pos, int slotId) {
        var json = new JSONObject();
        json["advertisedPosition"] = pos.Key;
        json["slotId"] = slotId;
        return Invoke("specifyAdvertisedPositionV1", json);
      }

      public static Guid SelectAdvertisedRole(Role role, int slotId) {
        var json = new JSONObject();
        json["advertisedRole"] = role.Key;
        json["slotId"] = slotId;
        return Invoke("specifyAdvertisedRoleV1", json);
      }

      public static Guid PickChampion(int championId) {
        var json = new JSONObject();
        json["championId"] = championId;
        return Invoke("pickChampionV2", json);
      }

      public static Guid PickPosition(Position pos, int slotId) {
        var json = new JSONObject();
        json["position"] = pos.Key;
        json["slotId"] = slotId;
        return Invoke("specifyPositionV2", json);
      }

      public static Guid PickRole(Role role, int slotId) {
        var json = new JSONObject();
        json["role"] = role.Key;
        json["slotId"] = slotId;
        return Invoke("specifyRoleV2", json);
      }

      public static Guid PickSkin(int skinId, bool isNewlyPurchased) {
        var json = new JSONObject();
        json["skinId"] = skinId;
        json["isNewlyPurchasedSkin"] = isNewlyPurchased;
        return Invoke("pickSkinV2", json);
      }

      public static Guid PickSpells(int spell1Id, int spell2Id) {
        var json = new JSONObject();
        json["spell1Id"] = spell1Id;
        json["spell2Id"] = spell2Id;
        return Invoke("pickSpellsV2", json);
      }

      public static Guid SoloAbandonLeaverBusterQueue(string accessTokenStr) {
        var json = new JSONObject();
        json["accessTokenStr"] = accessTokenStr;
        return Invoke("soloAbandonLeaverBusterLowPriorityQueueV1", json);
      }

      public static Guid SpecCandidates() {
        return Invoke("startSoloSpecPhaseV2");
      }

      public static Guid StartMatchmaking(string accessTokenStr = null) {
        var json = new JSONObject();
        if (accessTokenStr != null)
          json["accessTokenStr"] = accessTokenStr;
        return Invoke("startMatchmakingPhaseV2", json);
      }

      public static Guid UpdateLastSelectedSkin(int skinId, int championId) {
        var json = new JSONObject();
        json["skinId"] = skinId;
        json["championId"] = championId;
        return Invoke("updateLastSelectedSkinForChampionV1", json);
      }
    }

    public static class GameInvitationService {
      /// <summary>
      /// Creates a team builder lobby
      /// </summary>
      /// <param name="QueueId">The queue ID for the lobby (61) </param>
      /// <param name="UUID">The generated UUID of the lobby</param>
      /// <returns></returns>
      public static Task<LobbyStatus> CreateGroupFinderLobby(Int32 QueueId, String UUID) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "createGroupFinderLobby", QueueId, UUID);
      }

      /// <summary>
      /// Creates an arranged team lobby
      /// </summary>
      /// <param name="QueueId">The queue ID for the lobby</param>
      /// <returns></returns>
      public static Task<LobbyStatus> CreateArrangedTeamLobby(Int32 QueueId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "createArrangedTeamLobby", QueueId);
      }

      /// <summary>
      /// Creates an arranged team lobby for a co-op vs AI queue
      /// </summary>
      /// <param name="QueueId">The queue ID for the lobby</param>
      /// <param name="BotDifficulty">The difficulty for the bots</param>
      /// <returns></returns>
      public static Task<LobbyStatus> CreateArrangedBotTeamLobby(Int32 QueueId, string BotDifficulty) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "createArrangedBotTeamLobby", QueueId, BotDifficulty);
      }

      /// <summary>
      /// Creates an arranged team lobby for a ranked team game
      /// </summary>
      /// <param name="QueueId">The queue ID for the lobby</param>
      /// <param name="TeamName">The name of the team</param>
      /// <returns></returns>
      public static Task<LobbyStatus> CreateArrangedRankedTeamLobby(Int32 QueueId, string TeamName) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "createArrangedRankedTeamLobby", QueueId, TeamName);
      }

      /// <summary>
      /// Grants invite privelages to a summoner in an arranged lobby
      /// </summary>
      /// <param name="summonerId">The summoner to give invite privelages</param>
      /// <returns></returns>
      public static Task GrantInvitePrivileges(double summonerId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "grantInvitePrivileges", summonerId);
      }

      /// <summary>
      /// Transfers ownership of a lobby to a summoner in an arranged lobby
      /// </summary>
      /// <param name="summonerId">The summoner to transfer ownership to</param>
      /// <returns></returns>
      public static Task TransferOwnership(double summonerId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "transferOwnership", summonerId);
      }

      /// <summary>
      /// Invites a summoner to an arranged lobby
      /// </summary>
      /// <param name="summonerId">The summoner to invite</param>
      /// <returns></returns>
      public static Task Invite(double summonerId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "invite", summonerId);
      }

      /// <summary>
      /// Leaves an arranged lobby
      /// </summary>
      /// <returns></returns>
      public static Task Leave() {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "leave");
      }

      /// <summary>
      /// Accepts an invitation to an arranged lobby 
      /// </summary>
      /// <param name="InvitationId">The id of the invitation to accept, looks like INVID158928100</param>
      /// <returns></returns>
      public static Task<LobbyStatus> Accept(string InvitationId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "accept", InvitationId);
      }

      /// <summary>
      /// Declines an invitation to an arranged lobby 
      /// </summary>
      /// <param name="InvitationId">The id of the invitation to decline, looks like INVID158928100</param>
      /// <returns></returns>
      public static Task Decline(string InvitationId) {
        return InvokeAsync<LobbyStatus>("lcdsGameInvitationService", "decline", InvitationId);
      }
    }

    public static Task<T> InvokeAsync<T>(string destination, string method, params object[] argument) {
      try {
        return Client.RtmpConn.InvokeAsync<T>("my-rtmps", destination, method, argument);
      } catch (InvocationException e) {
        if (OnInvocationError != null)
          OnInvocationError(null, e);
        return null;
      }
    }

    public static async Task<string> GetAuthKey(String Username, String Password, String LoginQueue) {
      StringBuilder sb = new StringBuilder();
      string payload = "user=" + Username + ",password=" + Password;
      string query = "payload=" + payload;

      WebRequest con = WebRequest.Create(LoginQueue + "login-queue/rest/queue/authenticate");
      con.Method = "POST";

      Stream outputStream = con.GetRequestStream();
      outputStream.Write(Encoding.ASCII.GetBytes(query), 0, Encoding.ASCII.GetByteCount(query));

      WebResponse webresponse = await con.GetResponseAsync();
      Stream inputStream = webresponse.GetResponseStream();

      int c;
      while ((c = inputStream.ReadByte()) != -1)
        sb.Append((char) c);

      outputStream.Close();
      inputStream.Close();
      con.Abort();

      dynamic json = MFroehlich.Parsing.DynamicJSON.JSON.ParseObject(sb.ToString());

      string Status = json.status;

      if (Status == "QUEUE") { }

      if (json.ContainsKey("token"))
        return (string) json.token;
      else return null;
    }

    public static async Task<string> GetIpAddress() {
      StringBuilder sb = new StringBuilder();

      WebRequest con = WebRequest.Create("http://ll.leagueoflegends.com/services/connection_info");
      WebResponse response = await con.GetResponseAsync();

      int c;
      while ((c = response.GetResponseStream().ReadByte()) != -1)
        sb.Append((char) c);

      con.Abort();

      var json = MFroehlich.Parsing.DynamicJSON.JSON.ParseObject(sb.ToString());

      return (string) json["ip_address"];
    }

    public static SerializationContext RegisterObjects() {
      var context = new SerializationContext();
      Assembly LeagueClient = Assembly.GetExecutingAssembly();

      var x = from type in LeagueClient.GetTypes()
              where type.IsPublic && (
                string.Equals(type.Namespace, "LeagueClient.Logic.Riot.Platform", StringComparison.Ordinal) ||
                string.Equals(type.Namespace, "LeagueClient.Logic.Riot.Leagues", StringComparison.Ordinal) ||
                string.Equals(type.Namespace, "LeagueClient.Logic.Riot.Team", StringComparison.Ordinal))
              select type;

      foreach (Type type in x)
        context.Register(type);

      context.Register(typeof(PendingKudosDTO));
      context.RegisterAlias(typeof(Icon), "com.riotgames.platform.summoner.icon.SummonerIcon", true);
      context.RegisterAlias(typeof(StoreAccountBalanceNotification), "com.riotgames.platform.messaging.StoreAccountBalanceNotification", true);

      // Wack to make aram work
      context.RegisterAlias(typeof(PlayerParticipant), "com.riotgames.platform.reroll.pojo.AramPlayerParticipant", true);

      return context;
    }

    public enum PlayerSkill {
      BEGINNER,
      VETERAN,
      RTS_PLAYER
    }
  }
}
