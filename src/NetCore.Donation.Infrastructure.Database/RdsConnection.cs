using Npgsql;

namespace NetCore.Donation.Infrastructure.Database;

/// <summary>
/// Builds a Npgsql connection string from RDS_* environment variables so deploy
/// scripts never commit a PostgreSQL URI that contains a password.
/// </summary>
public static class RdsConnection
{
	public static string? TryFromEnvironment()
	{
		var host = Environment.GetEnvironmentVariable("RDS_ENDPOINT");
		var password = Environment.GetEnvironmentVariable("RDS_PASSWORD");
		if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(password))
		{
			return null;
		}

		var username = Environment.GetEnvironmentVariable("RDS_USERNAME");
		if (string.IsNullOrWhiteSpace(username))
		{
			username = "donationadmin";
		}

		var database = Environment.GetEnvironmentVariable("RDS_DATABASE");
		if (string.IsNullOrWhiteSpace(database))
		{
			database = "donation";
		}

		var builder = new NpgsqlConnectionStringBuilder
		{
			Host = host.Trim(),
			Port = 5432,
			Database = database.Trim(),
			Username = username.Trim(),
			Password = password,
			SslMode = SslMode.Require,
			TrustServerCertificate = true
		};

		return builder.ConnectionString;
	}

	public static bool IsMissing(string? connectionString) =>
		string.IsNullOrWhiteSpace(connectionString) ||
		connectionString.Contains("#{", StringComparison.Ordinal);
}
