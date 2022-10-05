FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["KerrysFlowersWebApp.csproj", "KerrysFlowersWebApp/"]
RUN dotnet restore "KerrysFlowersWebApp/KerrysFlowersWebApp.csproj"
COPY . .
WORKDIR "/src/KerrysFlowersWebApp"
RUN dotnet build "KerrysFlowersWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KerrysFlowersWebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KerrysFlowersWebApp.dll"]
