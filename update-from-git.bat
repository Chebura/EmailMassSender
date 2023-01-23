REM install git for windows
rename "EmailMassSender\usersettings.json" "usersettings.~son"
rename "EmailMassSender\appsettings.json" "appsettings.~son"
git pull
del ".\EmailMassSender\usersettings.json"
del ".\EmailMassSender\appsettings.json"
rename "EmailMassSender\usersettings.~son" "usersettings.json"
rename "EmailMassSender\appsettings.~son" "appsettings.json"
dotnet build