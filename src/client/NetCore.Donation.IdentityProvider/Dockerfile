#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Directory.Packages.props", "src/"]
COPY ["src/client/NetCore.Donation.IdentityProvider/NetCore.Donation.IdentityProvider.csproj", "src/client/NetCore.Donation.IdentityProvider/"]
COPY ["src/NetCore.Donation.Infrastructure.Database/NetCore.Donation.Infrastructure.Database.csproj", "src/NetCore.Donation.Infrastructure.Database/"]
COPY ["src/NetCore.Donation.Infrastructure.AuthenticationDatabase/NetCore.Donation.Infrastructure.AuthenticationDatabase.csproj", "src/NetCore.Donation.Infrastructure.AuthenticationDatabase/"]
COPY ["src/NetCore.Donation.Domain/NetCore.Donation.Domain.csproj", "src/NetCore.Donation.Domain/"]

RUN dotnet restore "src/client/NetCore.Donation.IdentityProvider/NetCore.Donation.IdentityProvider.csproj"
COPY . .

RUN dotnet build "src/client/NetCore.Donation.IdentityProvider/NetCore.Donation.IdentityProvider.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/client/NetCore.Donation.IdentityProvider/NetCore.Donation.IdentityProvider.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetCore.Donation.IdentityProvider.dll"]