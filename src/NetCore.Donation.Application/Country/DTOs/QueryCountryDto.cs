using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.Country.DTOs;

public sealed record QueryCountryDto
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("country-code")]
	public string CountryCode { get; set; } = string.Empty;

	[JsonPropertyName("alpha2")]
	public string Alpha2 { get; set; } = string.Empty;

	[JsonPropertyName("alpha3")]
	public string Alpha3 { get; set; } = string.Empty;
}