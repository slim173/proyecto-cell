#!/bin/sh
exec dotnet CellApp.dll --urls "http://+:${PORT:-8080}"
