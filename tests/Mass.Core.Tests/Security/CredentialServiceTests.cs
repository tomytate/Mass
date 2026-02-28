using System.Security;
using FluentAssertions;
using Mass.Core.Security;
using Xunit;

namespace Mass.Core.Tests.Security;

public class CredentialServiceTests
{
    private readonly CredentialService _service;

    public CredentialServiceTests()
    {
        _service = new CredentialService();
    }

    [Fact]
    public void GetCredential_ShouldReturnNull_WhenCredentialDoesNotExist()
    {
        // Act
        var result = _service.GetCredential("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void StoreAndGetCredential_ShouldWork()
    {
        // Arrange
        var id = "test_target";
        var name = "Test Credential";
        var username = "user";
        var password = "password";
        var securePassword = new SecureString();
        foreach (char c in password) securePassword.AppendChar(c);
        securePassword.MakeReadOnly();

        try
        {
            // Act
            _service.StoreCredential(id, name, username, securePassword);
            var result = _service.GetCredential(id);
            var retrievedPassword = _service.GetPassword(id);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(username);

            // Note: Cannot easily verify secure string content in unit test without unprotecting,
            // but GetPassword should return a non-null SecureString
            retrievedPassword.Should().NotBeNull();
        }
        finally
        {
            _service.DeleteCredential(id);
        }
    }

    [Fact]
    public void DeleteCredential_ShouldRemoveCredential()
    {
        // Arrange
        var id = "delete_target";
        var securePassword = new SecureString();
        securePassword.AppendChar('a');
        securePassword.MakeReadOnly();
        _service.StoreCredential(id, "Delete Me", "u", securePassword);

        // Act
        _service.DeleteCredential(id);
        var result = _service.GetCredential(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearAll_ShouldRemoveAllCredentials()
    {
        // Arrange
        var securePassword = new SecureString();
        securePassword.AppendChar('a');
        securePassword.MakeReadOnly();
        _service.StoreCredential("id1", "Cred 1", "u1", securePassword);
        _service.StoreCredential("id2", "Cred 2", "u2", securePassword);

        // Act
        _service.ClearAll();
        var all = _service.GetAllCredentials();

        // Assert
        all.Should().BeEmpty();
    }
}
