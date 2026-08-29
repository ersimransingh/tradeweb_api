#!/usr/bin/env bash
# Runs TradeWeb.API locally on this machine (.NET Core 3.1, EOL runtime).
# Sets up: local .NET 3.1 SDK, a locally-extracted libssl1.1 + libicu66
# (Ubuntu 24.04/Mint 22 ship libssl3 and libicu74, neither of which
# .NET Core 3.1 can use), and a minimal OpenSSL config.
#
# NOTE: DOTNET_SYSTEM_GLOBALIZATION_INVARIANT must NOT be set — in invariant
# mode Microsoft.Data.SqlClient throws NotSupportedException("Globalization
# Invariant Mode is not supported.") on every SqlConnection.Open(), so all
# database access silently fails.
set -euo pipefail

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export LD_LIBRARY_PATH="$HOME/.local/lib/libicu66/usr/lib/x86_64-linux-gnu:$HOME/.local/lib/libssl1.1/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH:-}"
# The system openssl.cnf references OpenSSL 3.x providers that libssl1.1
# cannot parse ("unknown module name"); point it at the repo's minimal config.
export OPENSSL_CONF="$(cd "$(dirname "$0")" && pwd)/openssl.minimal.cnf"

cd "$(dirname "$0")"

# `dotnet run` spawns the app in a subprocess that does not reliably inherit
# env overrides, so build then run the DLL directly.
dotnet build -v quiet
dotnet bin/Debug/netcoreapp3.1/TradeWeb.API.dll --urls "http://localhost:5080" "$@"
