namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Configurations;

public class AuthenticationServerConfiguration
{
	public string Audience { get; set; }
	public string CertificatePassword { get; set; }
	public string CertificatePath { get; set; }
	public string Issuer { get; set; }
	public string SecretKey { get; set; }
}
