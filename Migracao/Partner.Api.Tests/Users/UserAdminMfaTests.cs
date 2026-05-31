using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Partner.Api.Features.Users;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Tests.Users;

public sealed class UserAdminMfaTests
{
    [Fact]
    public async Task ResetMfa_ShouldReturnBadRequest_WhenActorIsTarget()
    {
        var endpoint = typeof(UserEndpoints)
            .GetMethod("ResetMfaAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var context = new DefaultHttpContext
        {
            User = TestUser.WithUserId(10, admin: true)
        };

        var action = (Func<int, ISqlConnectionFactory, HttpContext, CancellationToken, Task<IResult>>)Delegate.CreateDelegate(
            typeof(Func<int, ISqlConnectionFactory, HttpContext, CancellationToken, Task<IResult>>),
            endpoint!);

        var result = await action(10, new FakeSqlConnectionFactory(), context, CancellationToken.None);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>(result);
        var statusResult = (Microsoft.AspNetCore.Http.IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    private sealed class FakeSqlConnectionFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => new FakeDbConnection();
    }

    private sealed class FakeDbConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => "fake";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Open() { }
        public void Dispose() { }
    }
}

internal static class TestUser
{
    public static System.Security.Claims.ClaimsPrincipal WithUserId(int id, bool admin)
    {
        return new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(PartnerClaimTypes.UserId, id.ToString()),
            new System.Security.Claims.Claim(PartnerClaimTypes.Admin, admin ? "true" : "false")
        }, "test"));
    }
}