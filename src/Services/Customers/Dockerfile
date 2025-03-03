FROM mcr.microsoft.com/dotnet/sdk:latest as builder

WORKDIR /src

COPY ./.editorconfig ./

COPY ./src/Directory.Build.props ./
COPY ./src/Directory.Build.targets ./
COPY ./src/Directory.Packages.props ./
COPY ./src/Packages.props ./
COPY ./src/Services/Customers/Directory.Build.props ./Services/Customers/

# TODO: Using wildcard to copy all files in the directory.
COPY ./src/BuildingBlocks/BuildingBlocks.Abstractions/BuildingBlocks.Abstractions.csproj ./BuildingBlocks/BuildingBlocks.Abstractions/
COPY ./src/BuildingBlocks/BuildingBlocks.Core/BuildingBlocks.Core.csproj ./BuildingBlocks/BuildingBlocks.Core/
COPY ./src/BuildingBlocks/BuildingBlocks.Caching/BuildingBlocks.Caching.csproj ./BuildingBlocks/BuildingBlocks.Caching/
COPY ./src/BuildingBlocks/BuildingBlocks.Email/BuildingBlocks.Email.csproj ./BuildingBlocks/BuildingBlocks.Email/
COPY ./src/BuildingBlocks/BuildingBlocks.Integration.MassTransit/BuildingBlocks.Integration.MassTransit.csproj ./BuildingBlocks/BuildingBlocks.Integration.MassTransit/
COPY ./src/BuildingBlocks/BuildingBlocks.Logging/BuildingBlocks.Logging.csproj ./BuildingBlocks/BuildingBlocks.Logging/
COPY ./src/BuildingBlocks/BuildingBlocks.HealthCheck/BuildingBlocks.HealthCheck.csproj ./BuildingBlocks/BuildingBlocks.HealthCheck/
COPY ./src/BuildingBlocks/BuildingBlocks.Persistence.EfCore.Postgres/BuildingBlocks.Persistence.EfCore.Postgres.csproj ./BuildingBlocks/BuildingBlocks.Persistence.EfCore.Postgres/
COPY ./src/BuildingBlocks/BuildingBlocks.Persistence.Mongo/BuildingBlocks.Persistence.Mongo.csproj ./BuildingBlocks/BuildingBlocks.Persistence.Mongo/
COPY ./src/BuildingBlocks/BuildingBlocks.Resiliency/BuildingBlocks.Resiliency.csproj ./BuildingBlocks/BuildingBlocks.Resiliency/
COPY ./src/BuildingBlocks/BuildingBlocks.Security/BuildingBlocks.Security.csproj ./BuildingBlocks/BuildingBlocks.Security/
COPY ./src/BuildingBlocks/BuildingBlocks.Swagger/BuildingBlocks.Swagger.csproj ./BuildingBlocks/BuildingBlocks.Swagger/
COPY ./src/BuildingBlocks/BuildingBlocks.Validation/BuildingBlocks.Validation.csproj ./BuildingBlocks/BuildingBlocks.Validation/
COPY ./src/BuildingBlocks/BuildingBlocks.Web/BuildingBlocks.Web.csproj ./BuildingBlocks/BuildingBlocks.Web/
COPY ./src/BuildingBlocks/BuildingBlocks.Messaging.Persistence.Postgres/BuildingBlocks.Messaging.Persistence.Postgres.csproj ./BuildingBlocks/BuildingBlocks.Messaging.Persistence.Postgres/
COPY ./src/BuildingBlocks/BuildingBlocks.OpenTelemetry/BuildingBlocks.OpenTelemetry.csproj ./BuildingBlocks/BuildingBlocks.OpenTelemetry/

COPY ./src/Services/Customers/ECommerce.Services.Customers/ECommerce.Services.Customers.csproj ./Services/Customers/ECommerce.Services.Customers/
COPY ./src/Services/Customers/ECommerce.Services.Customers.Api/ECommerce.Services.Customers.Api.csproj ./Services/Customers/ECommerce.Services.Customers.Api/
COPY ./src/Services/Shared/ECommerce.Services.Shared/ECommerce.Services.Shared.csproj ./Services/Shared/ECommerce.Services.Shared/

RUN ls

# restore nuget packages
RUN dotnet restore ./Services/Customers/ECommerce.Services.Customers.Api/ECommerce.Services.Customers.Api.csproj

# Copy project files
COPY ./src/BuildingBlocks/ ./BuildingBlocks/
COPY ./src/Services/Customers/ECommerce.Services.Customers.Api/  ./Services/Customers/ECommerce.Services.Customers.Api/
COPY ./src/Services/Customers/ECommerce.Services.Customers/  ./Services/Customers/ECommerce.Services.Customers/
COPY ./src/Services/Shared/  ./Services/Shared/

# Build project with Release configuration
# and no restore, as we did it already
RUN dotnet build -c Release --no-restore  ./Services/Customers/ECommerce.Services.Customers.Api/ECommerce.Services.Customers.Api.csproj

WORKDIR /src/Services/Customers/ECommerce.Services.Customers.Api/
# Publish project to output folder
# and no build, as we did it already
RUN ls
RUN dotnet publish -c Release --no-restore  --no-build -o out

FROM mcr.microsoft.com/dotnet/aspnet:latest
# Setup working directory for the project
WORKDIR /app
COPY --from=builder /src/Services/Customers/ECommerce.Services.Customers.Api/out  .

#https://andrewlock.net/5-ways-to-set-the-urls-for-an-aspnetcore-app/
#https://swimburger.net/blog/dotnet/how-to-get-aspdotnet-core-server-urls
#https://tymisko.hashnode.dev/developing-aspnet-core-apps-in-docker-live-recompilat
#https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments

EXPOSE 80
ENV ASPNETCORE_URLS http://*:80
ENV ASPNETCORE_ENVIRONMENT docker
ENTRYPOINT ["dotnet", "ECommerce.Services.Customers.Api.dll"]
