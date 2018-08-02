FROM microsoft/dotnet:2.1-sdk AS dotnet-builder
WORKDIR /app
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR /app
COPY --from=dotnet-builder /app/out .
ENTRYPOINT ["dotnet", "EventHubs.Canary.Console.dll"]
