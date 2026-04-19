using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.auth;
using backend.models;
using backend.services.implementations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Backend.IntegrationTests;

public class TokenServiceTests
{
    private const string Issuer = "Testing.CoreData.API";
    private const string Audience = "Testing.CoreData.Client";
    private const string Key = "JWTSECRETFORTESTINGANDLONGENOUGHFOR!123!";
    private const int ExpiresMinutes = 60;

    private static TokenService CreateSut()
        => new(FakeJwtConfig());

    private static IConfiguration FakeJwtConfig()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:Key"] = Key,
            ["Jwt:ExpiresMinutes"] = ExpiresMinutes.ToString()
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void CreateAccessToken_ReturnsJwtWithExpectedClaims()
    {
        var sut = CreateSut();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "token.user@example.com",
            Permissions = (long)Permission.SuperAdmin
        };

        var token = sut.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Contain(Audience);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id.ToString());
        jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(user.Id.ToString());
        jwt.Claims.First(c => c.Type == "perm").Value.Should().Be(user.Permissions.ToString());
        jwt.Claims.First(c => c.Type == "email").Value.Should().Be(user.Email);
    }

    [Fact]
    public void CreateAccessToken_UsesHmacSha256AndValidatesSignature()
    {
        var sut = CreateSut();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sig.user@example.com",
            Permissions = (long)Permission.SuperAdmin
        };

        var token = sut.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10)
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
        principal.Should().NotBeNull();
    }

    [Fact]
    public void CreateAccessToken_SetsExpirationFromExpiresMinutes()
    {
        var sut = CreateSut();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "exp.user@example.com",
            Permissions = (long)Permission.SuperAdmin
        };

        var before = DateTime.UtcNow;
        var token = sut.CreateAccessToken(user);
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedFrom = before.AddMinutes(ExpiresMinutes - 1);
        var expectedTo = after.AddMinutes(ExpiresMinutes + 1);

        jwt.ValidTo.Should().BeAfter(expectedFrom);
        jwt.ValidTo.Should().BeBefore(expectedTo);
    }
}