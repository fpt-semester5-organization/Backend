using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace VeggieLife.Tests.Integration
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
    {
        private readonly MsSqlContainer _msSqlContainer;
        private readonly RedisContainer _redisContainer;

        public CustomWebApplicationFactory()
        {
            _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            _redisContainer = new RedisBuilder("redis:7.0").Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", _msSqlContainer.GetConnectionString()),
                    new KeyValuePair<string, string?>("Redis:ConnectionString", _redisContainer.GetConnectionString())
                });
            });
        }

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
            await _redisContainer.StartAsync();
        }

        new public async Task DisposeAsync()
        {
            await _msSqlContainer.DisposeAsync();
            await _redisContainer.DisposeAsync();
        }
    }
}
