FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY samples/document-approval/api/RuleGate.DocumentApproval.Api.csproj samples/document-approval/api/
RUN dotnet restore samples/document-approval/api/RuleGate.DocumentApproval.Api.csproj
COPY samples/document-approval/api samples/document-approval/api
RUN dotnet publish samples/document-approval/api/RuleGate.DocumentApproval.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RuleGate.DocumentApproval.Api.dll"]
