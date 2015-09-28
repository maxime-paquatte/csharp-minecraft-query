# csharp-minecraft-query
.Net Helper class to query a minecraft server


Simply copy/past the Status.cs source file in your project then call:

    var status  = Status.GetStatus("myserver.com");
    Console.WriteLine("Nb Players: " + status.NumPlayers);
    foreach (var player in status.Players) Console.WriteLine("\t" + player);


The available fields are :

- MessageOfTheDay
- Gametype
- GameId
- Version
- Plugins
- Map
- NumPlayers
- MaxPlayers
- HostPort
- HostIp
- Players

### Query
This method uses GameSpy4 protocol, and requires enabling `query` listener in your `server.properties` like this:

> *enable-query=true*<br>
> *query.port=25565*
