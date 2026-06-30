# Stage 1: Convert to regular project and build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
WORKDIR /src

# convert the sample to a regular project to reduce image size
COPY sample.cs .
RUN dotnet project convert sample.cs

RUN dotnet publish sample/ \
	-c Release \
	-r linux-musl-x64 \
	--self-contained true \
	-p:PublishAot=true \
	-p:StripSymbols=true \
	-p:InvariantGlobalization=true \
	-o ./publish

# Stage 2: Minimal runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build /src/publish/sample ./sample

ENTRYPOINT ["./sample"]