namespace UniAcademic.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "UniAcademic.Api";

    public string Audience { get; set; } = "UniAcademic.Clients";

    public string SigningKey { get; set; } = "ChangeThisSigningKey_AtLeast32Chars!";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 7;
}
