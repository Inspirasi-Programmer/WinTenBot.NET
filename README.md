# WinTenBot.NET
Official repository WinTenBot, written in .NET

# Build Requirement
- Microsoft Visual Studio 2019 (Community is free edition) or Jetbrains Rider (by Jetbrains)
- Net Core 3.1 SDK for ASP Net Core

# Deploy to Server
- VPS Installed nginx with domain name include HTTPS support (e.g https://yoursite.co.id)
- Net Core 3.1 Runtime
- Launch bot with `dotnet WinTenBot.dll` and get localhost:port
- Add config for reverse proxy to domain name, [here example](https://www.google.com/search?client=firefox-b-d&q=nginx+reverse+proxy+example)


Currently tested in [Zizi Beta Bot](t.me/MissZiziBetaBot)
