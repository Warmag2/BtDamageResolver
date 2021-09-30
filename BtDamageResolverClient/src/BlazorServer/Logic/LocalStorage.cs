using System.Threading.Tasks;
using Blazored.LocalStorage;
using Faemiyah.BtDamageResolver.Api.Entities;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Logic
{
    public class LocalStorage
    {
        private static string _localStorageVariableUserCredentials = "BtDamageResolverUserCredentials";
        private readonly ILocalStorageService _localStorageService;

        public LocalStorage(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public async Task<(bool, Credentials)> GetUserCredentials()
        {
            if (await _localStorageService.ContainKeyAsync(_localStorageVariableUserCredentials))
            {
                var credentials = await _localStorageService.GetItemAsync<Credentials>(_localStorageVariableUserCredentials);

                if (!string.IsNullOrWhiteSpace(credentials.Name))
                {
                    return (true, credentials);
                }
            }

            return (false, null);
        }

        public async Task SetUserCredentials(Credentials credentials)
        {
            await _localStorageService.SetItemAsync(_localStorageVariableUserCredentials, credentials);
        }

        public async Task RemoveUserCredentials()
        {
            await _localStorageService.RemoveItemAsync(_localStorageVariableUserCredentials);
        }
    }
}