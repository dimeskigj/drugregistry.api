FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY ./DrugRegistry.API/DrugRegistry.API.csproj ./DrugRegistry.API/
RUN dotnet restore ./DrugRegistry.API/DrugRegistry.API.csproj

COPY ./DrugRegistry.API ./DrugRegistry.API
RUN dotnet publish ./DrugRegistry.API/DrugRegistry.API.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/out .
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DrugRegistry.API.dll"]
