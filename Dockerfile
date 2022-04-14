FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /
ENV CSPROJFILE "src/Adescom CTM Billings/Adescom CTM Billings.csproj"
COPY ["Adescom CTM Billings/*.csproj", "./src/Adescom CTM Billings/"]
RUN dotnet restore "${CSPROJFILE}"
COPY . ./src/
RUN dotnet build "${CSPROJFILE}" -c Release -o /app

FROM build AS publish
RUN dotnet publish "${CSPROJFILE}" -c Release -o /app

FROM base AS final
RUN apt-get update && apt-get install -y \
    libgdiplus \
    xvfb \
    libfontconfig \
    wkhtmltopdf \
    libc6-dev \
    openssl \
    wget \
	locales \
    && apt-get clean
RUN wget --quiet https://github.com/rdvojmoc/DinkToPdf/raw/master/v0.12.4/64%20bit/libwkhtmltox.so -O /app/libwkhtmltox.so
RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && locale-gen
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US:en
ENV LC_ALL en_US.UTF-8
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Adescom CTM Billings.dll"]