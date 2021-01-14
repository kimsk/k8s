FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -f net5.0 -r linux-x64 --self-contained true -o /app

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
# Make port 80 available to the world outside this container
EXPOSE 80
COPY --from=build-env /app ./
RUN echo "{}" > /app/appsettings.json