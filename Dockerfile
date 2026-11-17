FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Usuarios.App.API/Usuarios.App.API.csproj", "Usuarios.App.API/"]
COPY ["UsuarioApp.Domain/UsuarioApp.Domain.csproj", "UsuarioApp.Domain/"]
COPY ["UsuariosApp.Infra.Data/UsuariosApp.Infra.Data.csproj", "UsuariosApp.Infra.Data/"]
COPY ["UsuariosApp.Infra.Messages/UsuariosApp.Infra.Messages.csproj", "UsuariosApp.Infra.Messages/"]
RUN dotnet restore "Usuarios.App.API/Usuarios.App.API.csproj"
COPY . .
RUN dotnet publish "Usuarios.App.API/Usuarios.App.API.csproj" -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Usuarios.App.API.dll"]
