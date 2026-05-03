#!/bin/bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
dotnet restore LogLens.sln
dotnet publish API/LogLens.API.csproj \
  -c Release \
  -o out \
  --no-restore \
  -p:UseAppHost=false