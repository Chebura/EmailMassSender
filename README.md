# About
Email Mass sender.

# End user license ageenment
Read [EULA](./EULA.txt).

# Pre-requesties
Install Microsoft .NET 5.0.4 for your OS:  
https://dotnet.microsoft.com/download/dotnet/5.0

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

# Consume

Read [instructions](./EmailMassSender/README.md). 

# Credits
Created by Kalianov Dmitry (https://chebura.github.io, http://mrald.narod.ru)