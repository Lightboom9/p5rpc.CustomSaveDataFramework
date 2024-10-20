# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p5r.CustomSaveDataFramework/*" -Force -Recurse
dotnet publish "./p5r.CustomSaveDataFramework.csproj" -c Release -o "$env:RELOADEDIIMODS/p5r.CustomSaveDataFramework" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location