FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src.sln", "./"]
COPY ["ReleaseSync.Console/ReleaseSync.Console.csproj", "ReleaseSync.Console/"]
COPY ["ReleaseSync.Application/ReleaseSync.Application.csproj", "ReleaseSync.Application/"]
COPY ["ReleaseSync.Domain/ReleaseSync.Domain.csproj", "ReleaseSync.Domain/"]
COPY ["ReleaseSync.Infrastructure/ReleaseSync.Infrastructure.csproj", "ReleaseSync.Infrastructure/"]

# 為了避免 restore 失敗，我們需要確保測試專案的資料夾結構也存在，或者只 restore 我們需要的專案
# 這裡直接針對 Console 專案進行 restore，它會自動處理相依性
RUN dotnet restore "ReleaseSync.Console/ReleaseSync.Console.csproj"

COPY . .
WORKDIR "/src/ReleaseSync.Console"
RUN dotnet build "ReleaseSync.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReleaseSync.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReleaseSync.Console.dll"]
