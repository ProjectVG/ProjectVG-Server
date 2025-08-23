namespace ProjectVG.Application.Models.Auth;

public class OAuth2Settings
{
    public const string SectionName = "OAuth";

    public List<string>? RedirectUris { get; set; }
    public List<OAuth2Client>? Clients { get; set; }
}

public class OAuth2Client
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public List<string> AllowedGrantTypes { get; set; } = new();
    public bool RequirePkce { get; set; } = true;
}
