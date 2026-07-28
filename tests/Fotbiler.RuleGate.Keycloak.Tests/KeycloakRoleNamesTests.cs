using System.Text.Json;

namespace Fotbiler.RuleGate.Keycloak.Tests;

public sealed class KeycloakRoleNamesTests
{
    [Fact]
    public void Normalization_MatchesSharedVectors()
    {
        var vectors = JsonSerializer.Deserialize<
            NormalizationVectors>(
                File.ReadAllText(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "KeycloakRoleNormalizationVectors.json")),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                })!;

        foreach (var component in vectors.Components)
        {
            Assert.Equal(
                component.Encoded,
                KeycloakRoleNames.EncodeComponent(
                    component.Value));
        }

        foreach (var role in vectors.Roles)
        {
            var normalized = role.Scope switch
            {
                "realm" =>
                    KeycloakRoleNames.RealmRole(
                        role.Role),
                "client" =>
                    KeycloakRoleNames.ClientRole(
                        role.ClientId!,
                        role.Role),
                _ => throw new InvalidOperationException(),
            };

            Assert.Equal(
                role.Normalized,
                normalized);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" role")]
    [InlineData("role ")]
    public void EncodeComponent_RejectsInvalidValues(
        string value)
    {
        Assert.Throws<ArgumentException>(
            () => KeycloakRoleNames
                .EncodeComponent(value));
    }

    private sealed record NormalizationVectors(
        ComponentVector[] Components,
        RoleVector[] Roles);

    private sealed record ComponentVector(
        string Value,
        string Encoded);

    private sealed record RoleVector(
        string Scope,
        string? ClientId,
        string Role,
        string Normalized);
}
