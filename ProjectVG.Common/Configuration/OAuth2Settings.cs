namespace ProjectVG.Common.Configuration
{
    public class OAuth2Settings
    {
        public bool Enabled { get; set; } = false;
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public bool AutoCreateUser { get; set; } = true;
        public string DefaultRole { get; set; } = "User";
        public List<string> Scopes { get; set; } = new List<string>();
    }

    public class OAuth2ProviderSettings
    {
        public Dictionary<string, OAuth2Settings> Providers { get; set; } = new Dictionary<string, OAuth2Settings>();
    }
}
