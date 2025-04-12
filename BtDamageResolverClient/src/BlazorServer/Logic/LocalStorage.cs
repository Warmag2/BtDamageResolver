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
    private readonly ProtectedSessionStorage _protectedSessionStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorage"/> class.
    /// </summary>
    /// <param name="localStorageService">The local storage service.</param>
    /// <param name="protectedSessionStorage">Protected session storage service.</param>
    public LocalStorage(ProtectedSessionStorage protectedSessionStorage)
    {
        _protectedSessionStorage = protectedSessionStorage;
    }

    /// <summary>
    /// Gets user credentials from local storage.
    /// </summary>
    /// <returns>A tuple with the success status of the fetch, and the credentials, if found.</returns>
    public async Task<Credentials> GetUserCredentials()
    {
        var credentialsResult = await _protectedSessionStorage.GetAsync<Credentials>(LocalStorageStoreName, LocalStorageVariableUserCredentials);

        if (credentialsResult.Success && !string.IsNullOrWhiteSpace(credentialsResult.Value.Name))
        {
            return credentialsResult.Value;
        }

        return null;
    }

    /// <summary>
    /// Sets user credentials.
    /// </summary>
    /// <param name="credentials">The credentials to set to.</param>
    /// <returns>A task which finishes when the credentials are set.</returns>
    public async Task SetUserCredentials(Credentials credentials)
    {
        await _protectedSessionStorage.SetAsync(LocalStorageStoreName, LocalStorageVariableUserCredentials, credentials);
    }

    /// <summary>
    /// Removes user credentials.
    /// </summary>
    /// <returns>A task which finishes when the credentials are removed.</returns>
    public async Task RemoveUserCredentials()
    {
        await _protectedSessionStorage.DeleteAsync(LocalStorageVariableUserCredentials);
    }
}