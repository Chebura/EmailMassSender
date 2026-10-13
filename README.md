# About
Email Mass sender.

# End user license ageenment
Read [EULA](./EULA.txt).

# Pre-requesties
Install Microsoft .NET 10 SDK for your OS:  
https://dotnet.microsoft.com/download/dotnet/10.0

# Build and run
Open cmd shell int `EmailMassSenderSln` directory and type:
```CMD
> dotnet restore
> dotnet build
```

Go to `./EmailMassSender/bin/Debug/net10.0/[CONFIG]`  

Open `appsettings.json`.

Configure `SmtpClient` section. Close file.

Open `usersettings.json`.

Configure `Groups`. Close file.

Just run `ems.exe` (or run `./dotnet ems.dll` for Linux).

# Consume

Read [instructions](./EmailMassSender/README.md). 

# Tests
```CMD
> dotnet test --solution EmailMassSenderSln.sln
```

See [EmailMassSender.Tests/README.md](./EmailMassSender.Tests/README.md) (in Russian).

# Credits
Created by Kalianov Dmitry (https://chebura.github.io, http://mrald.narod.ru)