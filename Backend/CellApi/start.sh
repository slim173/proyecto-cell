#!/bin/sh
exec dotnet CellApi.dll --urls "http://+:${PORT:-8080}"
