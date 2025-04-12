using System.Threading.Tasks;
using Blazored.LocalStorage;
using Faemiyah.BtDamageResolver.Api.Entities;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic;

/// <summary>
/// Local storage controller.
/// </summary>
public class LocalStorage
{
    private const string LocalStorageVariableUserCredentials = "BtDamageResolverUserCredentials";
    private readonly ILocalStorageService _localStorageService;
    private readonly ProtectedSessionStorage _protectedSessionStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorage"/> class.
    /// </summary>
    /// <param name="localStorageService">The local storage service.</param>
    public LocalStorage(ILocalStorageService localStorageService, ProtectedSessionStorage protectedSessionStorage)
    {
        _localStorageService = localStorageService;
        _protectedSessionStorage = protectedSessionStorage;
    }

    /// <summary>
    /// Gets user credentials from local storage.
    /// </summary>
    /// <returns>A tuple with the success status of the fetch, and the credentials, if found.</returns>
    public async Task<(bool Success, Credentials Credentials)> GetUserCredentials()
    {
        if (await _localStorageService.ContainKeyAsync(LocalStorageVariableUserCredentials))
        {
            var credentials = await _localStorageService.GetItemAsync<Credentials>(LocalStorageVariableUserCredentials);

            if (!string.IsNullOrWhiteSpace(credentials.Name))
            {
                return (true, credentials);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Sets user credentials.
    /// </summary>
    /// <param name="credentials">The credentials to set to.</param>
    /// <returns>A task which finishes when the credentials are set.</returns>
    public async Task SetUserCredentials(Credentials credentials)
    {
        await _localStorageService.SetItemAsync(LocalStorageVariableUserCredentials, credentials);
    }

    /// <summary>
    /// Removes user credentials.
    /// </summary>
    /// <returns>A task which finishes when the credentials are removed.</returns>
    public async Task RemoveUserCredentials()
    {
        await _localStorageService.RemoveItemAsync(LocalStorageVariableUserCredentials);
    }
}