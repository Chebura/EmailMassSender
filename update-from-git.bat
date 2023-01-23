REM install git for windows
ren .\EmailMassSender\usersettings.json .\EmailMassSender\usersettings.~son
ren .\EmailMassSender\appsettings.json .\EmailMassSender\appsettings.~son
git pull -f
del .\EmailMassSender\usersettings.json
del .\EmailMassSender\appsettings.json
ren .\EmailMassSender\usersettings.~son .\EmailMassSender\usersettings.json
ren .\EmailMassSender\appsettings.~son .\EmailMassSender\appsettings.json