FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
EXPOSE 5000
VOLUME /app/Logs

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EasyLog.LogServer/EasyLog.LogServer.csproj", "src/EasyLog.LogServer/"]
RUN dotnet restore "src/EasyLog.LogServer/EasyLog.LogServer.csproj"
COPY . .
WORKDIR "/src/src/EasyLog.LogServer"
RUN dotnet build "EasyLog.LogServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EasyLog.LogServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EasyLog.LogServer.dll"]
