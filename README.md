# Emulation Server
[![Build Emulation Server](https://github.com/Meltie2013/EmulationServer/actions/workflows/build.yml/badge.svg)](https://github.com/Meltie2013/EmulationServer/actions/workflows/build.yml)

Emulation Server is a C#/.NET 9 game-server emulator project built around a split-service architecture. The project separates authentication, world state, internal routing, map runtime services, instance runtime services, shared game logic, shared networking, and database access into dedicated projects so each part can be developed and tested independently.

The current focus is a legacy WoW-compatible emulator stack with RealmServer, WorldServer, ProxyServer, MapServer, and InstanceServer processes.

> This project is in active development. It is intended for learning, emulator research, and private development environments. It is not affiliated with, endorsed by, or connected to Blizzard Entertainment.

## Core features

### Multi-server architecture

- **RealmServer** handles the public login/auth socket, realm list responses, account authentication, configured realm metadata, and internal realm-status updates.
- **WorldServer** owns the public world socket, world login flow, player sessions, character list/create/delete flow, player logout, MOTD, world packet handling, and database-backed character/account state.
- **ProxyServer** acts as the private internal hub between backend services. It tracks connected internal peers, dependency state, latency, ping/pong health, and backend service status.
- **MapServer** runs non-instanced map services, tick loops, grid loading, player/map runtime state, and map service status reporting.
- **InstanceServer** runs instanced map services using the same runtime service model as MapServer. Instance services are currently disabled by default in `instanceserver.ini` until the runtime is expanded further.
- **Shared libraries** hold reusable configuration, logging, database, network protocol, game data, map, item, movement, chat, and command logic.

### Authentication and realm flow

- Realm authentication support with SRP6-style account verification.
- Realm list packet generation with configurable realm entries.
- Supported configured realm build values in `realmserver.ini`.
- WorldServer realm-status reporting back to RealmServer through the private internal listener.
- Separate public sockets for login and world traffic:
  - Realm login: `3724`
  - World/game realm: `8085`

### Internal networking

- Private service-to-service networking for Realm, World, Proxy, Map, and Instance services.
- Internal peer registration using a shared registration key.
- HMAC-style proof flow so the registration key is not sent directly across the socket.
- Configurable allowed internal server names per service.
- Reconnect handling for configured outgoing peers.
- TCP keep-alive and configurable socket buffer sizes.
- Shared internal packet framing and packet readers/writers.

### Health monitoring

- Internal ping/pong health checks.
- Latency tracking between backend services.
- Proxy-owned health evaluation for WorldServer, MapServer, InstanceServer, and individual map/instance services.
- Health states based on latency, missed pongs, load pressure, tick pressure, and stale status snapshots.
- Critical dependency handling for required backend services.
- Separate degraded and unhealthy thresholds in `proxyserver.ini`.

### Map and instance runtime

- Runtime map service manager.
- Per-map and per-instance service definitions.
- Service lifecycle support: start, stop, shutdown, and restart.
- Tick loop with configurable tick interval.
- Map grid loading support with `OnDemand` and `Preload` modes.
- Optional grid unload delay for idle grids.
- Optional startup grid warmup.
- Player map and zone runtime tracking/logging.
- Service snapshots for status reporting.
- Map and instance service load/tick health reporting.

### Logging and configuration

- INI-based configuration.
- Console/file/both logging modes.
- Configurable log folder and log file names.
- Enabled/disabled log type filters.
- Per-service configuration files:
  - `realmserver.ini`
  - `worldserver.ini`
  - `proxyserver.ini`
  - `mapserver.ini`
  - `instanceserver.ini`
- Graceful shutdown handling with configurable shutdown grace periods.

## Project layout

```text
EmulationServer/
├── database/
│   ├── base_account_schema.sql
│   ├── base_character_schema.sql
│   ├── base_world_schema.sql
│   └── updates/
├── src/
│   ├── EmulationServer.Core/
│   ├── EmulationServer.Database/
│   ├── EmulationServer.Game/
│   ├── EmulationServer.Network/
│   ├── EmulationServer.Shared/
│   ├── RealmServer/
│   ├── WorldServer/
│   ├── ProxyServer/
│   ├── MapServer/
│   └── InstanceServer/
├── tests/
│   └── EmulationServer.Tests/
├── tools/
│   ├── EmulationServer.Tools.Extraction/
│   └── MapDataTool/
└── EmulationServer.sln
```

## Requirements

- .NET 9 SDK
- MySQL or MariaDB
- A private/local development network for internal backend ports
- Valid game client data for extraction and local testing

## Build

Restore and build the full solution:

```bash
dotnet restore EmulationServer.sln
dotnet build EmulationServer.sln -c Release
```

Run tests:

```bash
dotnet test EmulationServer.sln -c Release
```

Database integration tests are skipped unless enabled with:

```bash
export EMULATIONSERVER_RUN_DATABASE_TESTS=true
dotnet test EmulationServer.sln -c Release
```

## Running the servers

For local development, start the services in this general order:

```bash
dotnet run --project src/ProxyServer/ProxyServer.csproj
dotnet run --project src/RealmServer/RealmServer.csproj
dotnet run --project src/WorldServer/WorldServer.csproj
dotnet run --project src/MapServer/MapServer.csproj
dotnet run --project src/InstanceServer/InstanceServer.csproj
```

Instance services are disabled by default. Enable them in:

```text
src/InstanceServer/instanceserver.ini
```

The default local ports are:

| Service | Purpose | Default port |
|---|---:|---:|
| RealmServer | Public login/auth socket | `3724` |
| RealmServer internal listener | Private realm-status listener | `5005` |
| ProxyServer | Private backend hub | `5000` |
| WorldServer internal listener | Private backend listener | `5002` |
| WorldServer client socket | Public game/world socket | `8085` |
| MapServer | Private map service listener | `5003` |
| InstanceServer | Private instance service listener | `5004` |

Only the client-facing ports should be exposed outside the machine or private LAN. Keep all internal backend ports private.

## Publishing

Publish an executable server into a deployment folder:

```bash
dotnet publish src/RealmServer/RealmServer.csproj -c Release -o /path/to/server/RealmServer
dotnet publish src/WorldServer/WorldServer.csproj -c Release -o /path/to/server/WorldServer
dotnet publish src/ProxyServer/ProxyServer.csproj -c Release -o /path/to/server/ProxyServer
dotnet publish src/MapServer/MapServer.csproj -c Release -o /path/to/server/MapServer
dotnet publish src/InstanceServer/InstanceServer.csproj -c Release -o /path/to/server/InstanceServer
```

Each server copies its matching `.ini` configuration file to the output folder.

## Configuration notes

Default local development configuration uses:

```text
RegistrationKey=dev-local-internal-key
BindAddress=127.0.0.1
```

Before any non-local deployment:

- Change every internal `RegistrationKey`.
- Bind internal services to private LAN/VPN addresses only.
- Keep ProxyServer, MapServer, InstanceServer, and internal Realm/World listeners behind a firewall.
- Expose only RealmServer's public login port and WorldServer's public world port when needed.
- Review database credentials and use a dedicated database user.
