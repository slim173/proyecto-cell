FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# --runtime linux-x64 garantiza que se copien libSkiaSharp.so y demás assets nativos
RUN dotnet publish CellApi.csproj -c Release -o /app/publish --runtime linux-x64

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CellApi.dll"]
