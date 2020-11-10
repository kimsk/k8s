FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
# Make port 80 available to the world outside this container
EXPOSE 80
COPY --from=build-env /app ./
RUN echo "{}" > /app/appsettings.json
ENTRYPOINT ["dotnet", "job-api.App.dll"]