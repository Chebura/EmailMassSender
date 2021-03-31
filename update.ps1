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
mkdir "./.chebura/bin/1.0.3"

copy "./.chebura/update/expanded/EmailMassSender-main/EmailMassSender/bin/Debug/net5.0/*.*" "./.chebura/bin/1.0.3"
