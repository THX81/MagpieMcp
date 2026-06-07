FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MagpieMcp.Api/MagpieMcp.Api.csproj MagpieMcp.Api/
RUN dotnet restore MagpieMcp.Api/MagpieMcp.Api.csproj

COPY MagpieMcp.Api/ MagpieMcp.Api/
RUN dotnet publish MagpieMcp.Api/MagpieMcp.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root user
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5066
EXPOSE 5066

ENTRYPOINT ["dotnet", "MagpieMcp.Api.dll"]
