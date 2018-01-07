# Cerberus
Custom GUI implementation of a Discord chat bot using [Discord.Net](https://github.com/RogueException/Discord.Net).

![alt tag](https://i.imgur.com/Pza774R.png)

**Toggles**

Log Chat - log all chat to a text file.

Log Users - log all unique users to have joined.

Ignore Bots - ignore input from all bots.

Ping Servers - check if any preset servers are online on a custom interval.

Safe Search - enable safe searching for the '!find' command.

Spam Control - prevent users from spamming the chat. (Experimental)

**Commands**

* !blacklist - list the blacklisted users, if any.
* !blacklist [@mention] - blacklist a user from Cerberus. (mod only)
* !find [search phrase] - random image from search phrase.
	- Can force a gif if 'gif' is included somewhere in search phrase.
	- 'Safesearch' can be toggled.
* !gamescom - check how many PUBG Gamescom Crates are for sale and their starting price.
* !help - display the help menu.
* !jail [@mention] - strip user roles and move them to the jail channel (mod only).
* !kick [@mention] - vote to kick another user from the server.
	- !yes - vote to kick user.
* !member - grant all users the member role since @everyone can't be manipulated easily and so !jail functions properly. (mod only)
* !ping [preset server name] - check server status.
* !pubg [player] [mode] - get PUBG rank for specified player in the specified game mode.
* !spam - enable/disable spam control. (mod only)
* !tits - natural tits! (birds).

Only a single timed vote can occur at a time to prevent issues. 

*WIP features*
* Dynamic server backup for locally hosted server files.
* Search / Filter console by Guild, Channel, User, etc.. 
* System to handle multiple timers / votes in multiple channels at once. 
* Dice rolling

*Disabled features*
* Welcome a user back after they have been offline and come back online, or join a guild voice channel after not have been in one previously. (Annoying)
* Prevent messages from being deleted. (Requires local cache)

-----------------------------------

**CHANGELOG**

*Latest version:* 0.3

* Visual changes.
* New Settings window.
	- Moved togglable settings here. 
		- New toggle to ignore all Discord bot input and messages. (Was on by default)
		- Ping custom list of preset server IP addresses by command or timer. 
	- Support for custom bot tokens. (Prompted on first start)
* Support for resizing main window vertically.
* Better console formatting. 
	- Colors
	- Text wrapping
	- Delete messages (Discord and console)
* Search and Sort support for the Guilds, Channels, and Users list boxes.
	- Users box is sorted first by online status, then alphabetically.
* Separate text box for displaying selected channel/user being messaged, which cannot be edited or deleted unlike previously.
* Get PUBG player ranks in any playlist with the new !pubg command using [PUBGSharp](https://github.com/eklypss/PUBGSharp).
* Track the PUBG Gamescom crate with the new !gamescom command.
* Safe search for !find command is bypassed in text channels marked 'nsfw'.
* Update !ping command.
* Fixed reconnect crash.
* Resource optimization.

0.2.0.1

* Right click console box item to view timestamp and/or copy text to clipboard.

0.2.0.0

* Update to Discord.Net version 1.0.0-rc
* Convert from console format to GUI.
