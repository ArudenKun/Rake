namespace Rake.Core.YtDlp.Models.Auth;

/// <summary>
/// Represents authentication credentials.
/// </summary>
/// <remarks>
/// These credentials are validated at construction time to ensure they are not null or empty.
/// </remarks>
public sealed record YtdlpAuthentication
{
    /// <summary>Username used for authentication.</summary>
    internal string Username { get; }

    /// <summary>Password used for authentication.</summary>
    internal string Password { get; }

    internal YtdlpAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Invalid credentials.");

        Username = username;
        Password = password;
    }
}
