FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["/ComponentsLibrary/ComponentsLibrary.csproj", "build/"]
COPY ["/SharedLibrary/SharedLibrary.csproj", "build/"]
COPY ["/KerrysFlowersWebApp/KerrysFlowersWebApp.csproj", "build/"]

RUN dotnet restore "build/ComponentsLibrary.csproj"
RUN dotnet restore "build/SharedLibrary.csproj"
RUN dotnet restore "build/KerrysFlowersWebApp.csproj"

COPY . .
WORKDIR "/src/build"
RUN dotnet build "ComponentsLibrary.csproj" -c Release -o /app/build
RUN dotnet build "SharedLibrary.csproj" -c Release -o /app/build
RUN dotnet build "KerrysFlowersWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KerrysFlowersWebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KerrysFlowersWebApp.dll"]
