FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["SharedLibrary/SharedLibrary.csproj", "."]
RUN dotnet restore "SharedLibrary.csproj"
COPY . .
RUN dotnet build "SharedLibrary.csproj" -c Release -o /app/build

COPY ["ComponentsLibrary/ComponentsLibrary.csproj", "."]
RUN dotnet restore "ComponentsLibrary.csproj"
COPY . .
RUN dotnet build "ComponentsLibrary.csproj" -c Release -o /app/build

COPY ["KerrysFlowersWebApp/KerrysFlowersWebApp.csproj", "."]
RUN dotnet restore "KerrysFlowersWebApp.csproj"
COPY . .
RUN dotnet build "KerrysFlowersWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SharedLibrary.csproj" -c Release -o /app/publish
RUN dotnet publish "ComponentsLibrary.csproj" -c Release -o /app/publish
RUN dotnet publish "KerrysFlowersWebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KerrysFlowersWebApp.dll"]