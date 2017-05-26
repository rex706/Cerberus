# Cerberus
GUI implementation of a Discord chat bot using [Discord.Net](https://github.com/RogueException/Discord.Net).

![alt tag](http://i.imgur.com/CnSWJyo.png)

**Toggles**

Log Chat - log all chat to a text file.

Log Users - log all unique users to have joined.

Ping Servers - check if Minecraft/Starbound server is online.

Safe Search - enable safe searching for the '!find' command.

Spam Control - prevent users from spamming the chat. (Experimental)

**Commands**

* !help - display the help menu.
* !tits - natural tits! (birds).
* !find [search phrase] - random image from search phrase.
* !minecraft - minecraft server status. 
* !starbound - starbound server status.
* !jail [@mention] - strip user roles and move them to the jail channel (mod only).
* !kick [@mention] - vote to kick another user from the server.
	- !yes - vote to kick user.
* !blacklist - list the blacklisted users, if any.
* !blacklist [@mention] - blacklist a user from Cerberus. (mod only)
* !spam - enable/disable spam control. (mod only)
* !member - grant all users the member role to act as the new @everyone role so !jail works. (mod only)

Will also welcome new users to server.
Only a single timed vote can occur at a time to prevent issues. 

*Disabled features*
* Welcome a user back after they have been offline and come back online, or join a guild voice channel after not have been in one previously.
* Prevent messages from being deleted.

-----------------------------------

**CHANGELOG**

*Latest version:* 0.2.0.1

* Right click main listbox item to view timestamp and/or copy text to clipboard.

0.2.0.0

* Update to Discord.Net version 1.0.0-rc
* Convert console to GUI.
