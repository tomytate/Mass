using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProPXEServer.Client.Services;

public class AuthService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider) {
    private const string TokenKey = "authToken";
    private UserInfo? _currentUser;

    public record LoginResponse(string Token, UserInfo User);
    public record UserInfo(string Id, string Email, bool IsSubscribed, string? SubscriptionStatus, DateTime? SubscriptionExpiry);

    public async Task<bool> LoginAsync(string email, string password) {
        try {
            var response = await httpClient.PostAsJsonAsync("auth/login", new {
                email,
                password
            });

            if (!response.IsSuccessStatusCode) {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token != null) {
                await localStorage.SetItemAsStringAsync(TokenKey, result.Token);
                ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
                _currentUser = result.User;
                return true;
            }

            return false;
        }
        catch {
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password) {
        try {
            var response = await httpClient.PostAsJsonAsync("auth/register", new {
                email,
                password
            });

            if (!response.IsSuccessStatusCode) {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Token != null) {
                await localStorage.SetItemAsStringAsync(TokenKey, result.Token);
                ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
                _currentUser = result.User;

                return true;
            }

            return false;
        }
        catch {
            return false;
        }
    }

    public async Task LogoutAsync() {
        await localStorage.RemoveItemAsync(TokenKey);
        ((CustomAuthStateProvider)authStateProvider).NotifyUserLogout();
        _currentUser = null;
    }

    public async Task<string?> GetTokenAsync() {
        return await localStorage.GetItemAsStringAsync(TokenKey);
    }

    public async Task<bool> IsAuthenticatedAsync() {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<UserInfo?> GetCurrentUserAsync() {
        if (_currentUser != null) {
            return _currentUser;
        }

        try {
            var response = await httpClient.GetAsync("auth/me");
            if (response.IsSuccessStatusCode) {
                _currentUser = await response.Content.ReadFromJsonAsync<UserInfo>();
                return _currentUser;
            }
        }
        catch { }

        return null;
    }
}


