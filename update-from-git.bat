REM install git for windows
rename ".\EmailMassSender\usersettings.json" ".\EmailMassSender\usersettings.~son"
rename ".\EmailMassSender\appsettings.json" ".\EmailMassSender\appsettings.~son"
git pull -f
del ".\EmailMassSender\usersettings.json"
del ".\EmailMassSender\appsettings.json"
rename ".\EmailMassSender\usersettings.~son" ".\EmailMassSender\usersettings.json"
rename ".\EmailMassSender\appsettings.~son" ".\EmailMassSender\appsettings.json"
dotnet build