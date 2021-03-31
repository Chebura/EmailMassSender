cd $env:USERPROFILE
mkdir ./.chebura/update/ -Force
cd ./.chebura/update/
rm ./main.zip -Force
rm ./expanded -Force -Recurse

Invoke-WebRequest -Uri "https://github.com/Chebura/EmailMassSender/archive/refs/heads/main.zip" -OutFile "./main.zip"

Expand-Archive main.zip ./expanded/

cd ./expanded/EmailMassSender-main

dotnet restore

dotnet build

cd $env:USERPROFILE
copy "./.chebura/bin/EmailMassSender/appsettings.json" "./.chebura/update" 
copy "./.chebura/bin/EmailMassSender/usersettings.json" "./.chebura/update" 

rm "./.chebura/bin/EmailMassSender" -Force -Recurse
mkdir "./.chebura/bin/EmailMassSender" -Force

copy "./.chebura/update/expanded/EmailMassSender-main/EmailMassSender/bin/Debug/net5.0/*.*" "./.chebura/bin/EmailMassSender"

copy "./.chebura/update/appsettings.json" "./.chebura/bin/EmailMassSender"
copy "./.chebura/update/usersettings.json" "./.chebura/bin/EmailMassSender"
