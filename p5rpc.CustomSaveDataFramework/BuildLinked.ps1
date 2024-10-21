# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p5rpc.CustomSaveDataFramework/*" -Force -Recurse
dotnet publish "./p5rpc.CustomSaveDataFramework.csproj" -c Release -o "$env:RELOADEDIIMODS/p5rpc.CustomSaveDataFramework" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location