FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build

WORKDIR /src
COPY src/backend/ ./src/backend/
RUN dotnet restore src/backend/Homework2.Api/Homework2.Api.csproj
RUN dotnet publish src/backend/Homework2.Api/Homework2.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final

WORKDIR /app
COPY --from=build /app/publish/ ./
RUN mkdir -p /app/data && chown -R app:app /app/data

USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Homework2.Api.dll"]
