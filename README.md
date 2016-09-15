# Cerberus				![alt tag](http://i.imgur.com/Z3cuEJA.png?1)
Discord chat bot with some useful user commands.

-Requires DiscordNet Nuget package-

Commands
* !cat ---------------------------------- generate random cat picture.
* !dog ---------------------------------- generate random dog picture.
* !tits ---------------------------------- natural tits! (birds).
* !gimme [search phrase] ----------------- random image from search phrase.
* !region -------------------------------- get discord server region.
* !minecraft ----------------------------- minecraft server status. 
* !starbound ----------------------------- starbound server status.
* !jail [username] [discriminator] ------- vote to strip user's roles and jail them to the jail channel.
* !kick [username] [discriminator] ------- vote to kick another user from the server.
* !member -------------------------------- grant all users the member role to act as the new @everyone role so !jail works. (admin only)
	- member command not yet working. need to save member and noob role to specific values from the start to be accessed whenever.

Also auto pings minecraft and starbound servers every hour and issues a server backup if found online.
Will also welcome new users to server.
Only a single timed vote can occur at a time to prevent issues. 


Disabled features
* Welcome a user back afte they have been offline and come back online, or join a guild voice channel after not have been in one previously.
* Prevent messages from being deleted.