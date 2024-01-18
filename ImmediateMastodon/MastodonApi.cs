using System.Net.Http.Headers;
using System.Reflection;
using Mastonet;
using Mastonet.Entities;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ImmediateMastodon;

public class MastodonApi(Config config) : IHostedService {
    private static ILogger Logger = Log.ForContext<MastodonApi>();

    public AuthenticationClient? Auth;
    public MastodonClient? Api;
    public HttpClient Client = new();

    public event Action? OnInitialized;
    public event Action? OnLogin;

    public async Task StartAsync(CancellationToken cancellationToken) {
        this.Client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("ImmediateMastodon",
                                       Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"));

        if (config.LastAccount is not null && config.AccountAuth.TryGetValue(config.LastAccount, out var token)) {
            var instance = config.LastAccount.Split('@')[1];
            await this.Create(instance);
            await LoginWithAccessToken(token);
            this.OnLogin?.Invoke();
        }

        this.OnInitialized?.Invoke();
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        this.Client.Dispose();
        return Task.CompletedTask;
    }

    public async Task Create(string instance) {
        this.Auth = new AuthenticationClient(instance, this.Client);
        if (config.OAuthApps.TryGetValue(instance, out var existingApp)) {
            this.Auth.AppRegistration = existingApp;
        } else {
            var app = await this.Auth.CreateApp("ImmediateMastodon", scope: [
                GranularScope.Read,
                GranularScope.Write,
                GranularScope.Follow,
                GranularScope.Push
            ]);
            config.OAuthApps.Add(instance, app);
            config.Save();
        }

        if (
            config.LastAccount is not null
            && config.AccountAuth.TryGetValue(config.LastAccount, out var auth)
        ) {
            try {
                await LoginWithAccessToken(auth);
            } catch (Exception e) {
                Logger.Error(e, "Failed to login with access token");
            }
        }
    }

    public void OpenOAuthUrl() {
        if (this.Auth is null) return;
        Utils.OpenUrl(this.Auth.OAuthUrl());
    }

    public async Task LoginWithCode(string code) {
        if (this.Auth is null) return;
        var auth = await this.Auth.ConnectWithCode(code);
        await LoginWithAccessToken(auth);
    }

    private async Task LoginWithAccessToken(Auth auth) {
        if (this.Auth is null) return;

        this.Api = new MastodonClient(this.Auth.Instance, auth.AccessToken, this.Client);
        var me = await this.Api.GetCurrentUser();
        var account = me.UserName + "@" + this.Auth.Instance;

        config.AccountAuth[account] = auth;
        config.LastAccount = account;
        config.Save();
    }
}
