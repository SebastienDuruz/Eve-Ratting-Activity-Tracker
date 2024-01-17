FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

RUN apt-get update
RUN apt-get install wkhtmltopdf -y

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["EveRAT.csproj", "./"]
RUN dotnet restore "EveRAT.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "EveRAT.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EveRAT.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EveRAT.dll"]
