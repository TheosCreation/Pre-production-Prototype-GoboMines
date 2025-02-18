using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : Singleton<SessionManager>
{
    ISession activeSession;

    ISession ActiveSession
    {
        get => activeSession;
        set
        {
            activeSession = value;
            Debug.Log($"Active Session: {activeSession}");
        }
    }

    const string playerNamePropertyKey = "playerName";

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Sign in anomously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void RegisterSessionEvents()
    {
        ActiveSession.Changed += OnSessionChanged;
    }

    void DeRegisterSessionEvents()
    {
        ActiveSession.Changed -= OnSessionChanged;
    }

    async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        // Custom game specfics stuff like level, name, etc
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        return new Dictionary<string, PlayerProperty> { { playerNamePropertyKey, playerNameProperty } };
    }

    public async Task StartSessionAsHost()
    {
        var playerProperties = await GetPlayerProperties();

        var options = new SessionOptions
        {
            MaxPlayers = 2,
            IsLocked = false,
            IsPrivate = false,
            PlayerProperties = playerProperties,
        }.WithRelayNetwork(); // peer to peer connection for now, lets use matchmaker or server hosted later on

        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
    }

    public async Task JoinSessionById(string sessionId)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
        Debug.Log($"Session {ActiveSession.Id} joined!");
    }

    public async Task JoinSessionByCode(string sessionCode)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Session {ActiveSession.Id} joined!");
    }

    public async Task KickPlayer(string playerId)
    {
        if (!ActiveSession.IsHost) return;
        await ActiveSession.AsHost().RemovePlayerAsync(playerId);
    }

    public async Task<IList<ISessionInfo>> QuerySessions()
    {
        var sessionQueryOptions = new QuerySessionsOptions();
        QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
        return results.Sessions;
    }

    public async Task LeaveSession()
    {
        if (ActiveSession != null)
        {
            DeRegisterSessionEvents();
            try
            {
                await ActiveSession.LeaveAsync();
            }
            catch
            {
            }
            finally
            {
                ActiveSession = null;
            }
        }
    }

    private void OnSessionChanged()
    {
    }
}
