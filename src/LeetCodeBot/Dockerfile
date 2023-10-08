FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Src/LeetCodeBot/LeetCodeBot.csproj", "Src/LeetCodeBot/"]
RUN dotnet restore "Src/LeetCodeBot/LeetCodeBot.csproj"
COPY . .
WORKDIR "/src/Src/LeetCodeBot"
RUN dotnet build "LeetCodeBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LeetCodeBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeetCodeBot.dll"]
