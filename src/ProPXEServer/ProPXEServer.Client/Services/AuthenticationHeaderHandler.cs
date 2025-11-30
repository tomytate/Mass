using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace ProPXEServer.Client.Services;

public class AuthenticationHeaderHandler(ILocalStorageService localStorage) : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        
        var token = await localStorage.GetItemAsStringAsync("authToken");
        if (!string.IsNullOrEmpty(token)) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

