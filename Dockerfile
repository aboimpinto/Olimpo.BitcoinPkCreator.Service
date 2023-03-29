FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /source
ADD BitcoinPkCreatorWorker/ /source

RUN dotnet restore && \
    dotnet publish --configuration Release --output /app --no-restore


FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir /settings
# ENTRYPOINT ["dotnet", "BitcoinPkCreatorWorker.dll"]
# CMD ["dotnet", "BitcoinPkCreatorWorker.dll"]