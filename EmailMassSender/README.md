# About
Email Mass sender.

# End user license ageenment
Read [EULA](./EULA.txt).

# Pre-requesties
## Windows
Install Microsoft .NET 5.0.4:  
https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-5.0.4-windows-x64-installer

## Linux
Follow instructions:  
https://docs.microsoft.com/en-us/dotnet/core/install/linux

# Build and run
Open cmd shell int `EmailMassSenderSln` directory and type:
```CMD
> dotnet restore
> dotnet build
```

Go to `./EmailMassSender/bin/Debug/net5.0/[CONFIG]`  

Open `appsettings.json`.

Configure `SmtpClient` section. Close file.

Open `usersettings.json`.

Configure `Groups`. Close file.

Just run `ems.exe` (or run `./dotnet ems.dll` for Linux).

# Credits
Created by Kalianov Dmitry (https://chebura.github.io, http://mrald.narod.ru)