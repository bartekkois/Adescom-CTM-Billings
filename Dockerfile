FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /
ENV CSPROJFILE "src/Adescom CTM Billings/Adescom CTM Billings.csproj"
COPY ["Adescom CTM Billings/*.csproj", "./src/Adescom CTM Billings/"]
RUN dotnet restore "${CSPROJFILE}"
COPY . ./src/
RUN dotnet build "${CSPROJFILE}" -c Release -o /app

FROM build AS publish
RUN dotnet publish "${CSPROJFILE}" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Adescom CTM Billings.dll"]
