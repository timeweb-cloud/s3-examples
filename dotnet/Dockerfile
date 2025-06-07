# Stage 1: Convert to regular project and build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# convert the sample to a regular project to reduce image size
COPY sample.cs .
RUN dotnet project convert sample.cs

RUN dotnet publish sample/ \
        -c Release 

# Stage 2: Use ~<100mb image and run the app
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview-alpine AS runtime
WORKDIR /app

COPY --from=build /src/sample/bin/Release/net10.0/publish .

ENTRYPOINT ["./sample"]