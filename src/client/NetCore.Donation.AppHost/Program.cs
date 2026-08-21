using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Stable local password so WithDataVolume() survives AppHost restarts (Aspire-generated
// passwords otherwise drift from the persisted volume and postgres never becomes healthy).
var postgresPassword = builder.AddParameter(
    "postgres-password",
    secret: true);

// Stable password (appsettings.Development Parameters:postgres-password) so a data
// volume stays usable across AppHost restarts. If postgres stays Waiting forever,
// delete Docker volume netcore.donation.apphost-*-postgres-data once and restart.
var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume();

var appDb = postgres.AddDatabase("netcore-donation-db");

var redis = builder.AddRedis("redis");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("minio-data", "/data")
    .WithHttpHealthCheck("/minio/health/live", endpointName: "api");

var minioApi = minio.GetEndpoint("api");

var migrator = builder.AddProject<Projects.NetCore_Donation_Migration>("migration")
    .WithReference(appDb)
    .WithReference(redis)
    .WaitFor(appDb)
    .WaitFor(minio)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__Provider", "postgresql")
    .WithEnvironment("Database__MigrationsAssembly", "NetCore.Donation.Infrastructure.Database")
    .WithEnvironment("ObjectStorage__BucketName", "receipts")
    .WithEnvironment("ObjectStorage__AccessKey", "minioadmin")
    .WithEnvironment("ObjectStorage__SecretKey", "minioadmin")
    .WithEnvironment("ObjectStorage__ForcePathStyle", "true")
    .WithEnvironment("ObjectStorage__CreateBucketIfNotExists", "true")
    .WithEnvironment("ObjectStorage__Region", "us-east-1")
    .WithEnvironment("ObjectStorage__ServiceUrl", minioApi)
    .WithArgs("-m", "-s")
    .ExcludeFromManifest();

var api = builder.AddProject<Projects.NetCore_Donation_Api>("api")
    .WithReference(appDb)
    .WithReference(redis)
    .WaitFor(migrator)
    .WaitFor(minio)
    .WithEnvironment("Database__Provider", "postgresql")
    .WithEnvironment("Database__MigrationsAssembly", "NetCore.Donation.Infrastructure.Database")
    .WithEnvironment("ObjectStorage__BucketName", "receipts")
    .WithEnvironment("ObjectStorage__AccessKey", "minioadmin")
    .WithEnvironment("ObjectStorage__SecretKey", "minioadmin")
    .WithEnvironment("ObjectStorage__ForcePathStyle", "true")
    .WithEnvironment("ObjectStorage__CreateBucketIfNotExists", "true")
    .WithEnvironment("ObjectStorage__Region", "us-east-1")
    .WithEnvironment("ObjectStorage__ServiceUrl", minioApi)
    .WithHttpEndpoint(port: 6000, name: "api-http")
    .WithHttpsEndpoint(port: 6001, name: "api-https");

var ui = builder.AddProject<Projects.NetCore_Donation_UI>("ui")
    .WithReference(api)
    .WithEnvironment("ApiBaseAddress", () => api.GetEndpoint("api-http").Url)
    .WithHttpEndpoint(port: 6010, name: "ui-http")
    .WithHttpsEndpoint(port: 6011, name: "ui-https");

var admin = builder.AddProject<Projects.NetCore_Donation_Admin>("admin")
    .WithReference(api)
    .WithEnvironment("ApiBaseAddress", () => api.GetEndpoint("api-http").Url)
    .WithHttpEndpoint(port: 6020, name: "admin-http")
    .WithHttpsEndpoint(port: 6021, name: "admin-https");

await builder.Build().RunAsync();
