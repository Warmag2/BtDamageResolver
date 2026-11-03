using System;
using System.Threading.Tasks;
using Faemiyah.BtDamageResolver.Api.Entities;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;

/// <summary>
/// Local storage controller.
/// </summary>
public class LocalStorage
{
    private const string LocalStorageStoreName = "BtDamageResolverClient";
    private const string LocalStorageVariableUserCredentials = "BtDamageResolverUserCredentials";
    private const string LocalStorageVariableClientId = "BtDamageResolverClientId";
    private readonly ProtectedLocalStorage _protectedLocalStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorage"/> class.
    /// </summary>
    /// <param name="protectedLocalStorage">Protected local storage service.</param>
    public LocalStorage(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    /// <summary>
    /// Gets user credentials from local storage.
    /// </summary>
    /// <returns>A tuple with the success status of the fetch, and the credentials, if found.</returns>
    public async Task<Credentials> GetUserCredentials()
    {
        var credentialsResult = await _protectedLocalStorage.GetAsync<Credentials>(LocalStorageStoreName, LocalStorageVariableUserCredentials);

        if (credentialsResult.Success && !string.IsNullOrWhiteSpace(credentialsResult.Value.Name))
        {
            return credentialsResult.Value;
        }

        return null;
    }

    /// <summary>
    /// Sets user credentials.
    /// </summary>
    /// <remarks>
    /// Only the user name and the authentication token are persisted. The password is never stored,
    /// so a compromised browser store cannot leak it. Reconnecting relies on the authentication token.
    /// </remarks>
    /// <param name="credentials">The credentials to set to.</param>
    /// <returns>A task which finishes when the credentials are set.</returns>
    public async Task SetUserCredentials(Credentials credentials)
    {
        var persistedCredentials = new Credentials
        {
            Name = credentials.Name,
            AuthenticationToken = credentials.AuthenticationToken
        };

        await _protectedLocalStorage.SetAsync(LocalStorageStoreName, LocalStorageVariableUserCredentials, persistedCredentials);
    }

    /// <summary>
    /// Removes user credentials.
    /// </summary>
    /// <returns>A task which finishes when the credentials are removed.</returns>
    public async Task RemoveUserCredentials()
    {
        await _protectedLocalStorage.DeleteAsync(LocalStorageVariableUserCredentials);
    }

    /// <summary>
    /// Gets the persistent client id for this browser, generating and storing one if it does not yet exist.
    /// </summary>
    /// <remarks>
    /// The client id identifies this browser to the server so that each browser can hold its own session.
    /// It is deliberately kept separate from the user credentials so that it survives logging out.
    /// </remarks>
    /// <returns>The persistent client id for this browser.</returns>
    public async Task<Guid> GetClientId()
    {
        var clientIdResult = await _protectedLocalStorage.GetAsync<Guid>(LocalStorageStoreName, LocalStorageVariableClientId);

        if (clientIdResult.Success && clientIdResult.Value != Guid.Empty)
        {
            return clientIdResult.Value;
        }

        var clientId = Guid.NewGuid();
        await _protectedLocalStorage.SetAsync(LocalStorageStoreName, LocalStorageVariableClientId, clientId);

        return clientId;
    }
}