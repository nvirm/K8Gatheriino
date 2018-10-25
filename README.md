# K8Gatheriino

![alt text](https://github.com/kitsun8/K8Gatheriino/blob/master/screenshots/gatheriino3.PNG)


A bot for Discord that helps people to form pickup games (PUGs).
Originally for Overwatch, but as the team size can be changed from settings, works with any team based game.

# What it does
0. Reads settings from appsettings.json (sample found in 'settings/appsettings.json')
1. Gathers <INSERT QUEUE SIZE> amount of people to a queue
2. Asks players to Ready up inside <INSERT TIME> seconds of time.
3. Kicks unreadied players out of the queue.
4. Decides 2 random team captains. Captainship requires <INSERT THRESHOLD> games to be played.
5. Allows captains to pick <INSERT QUEUE SIZE> amount of players to teams, 1 by 1.
6. Announces teams when all players have been picked.

# Extra features
1. Keeps track of played games for users
2. Keeps track of first picks of users
3. Keeps track of last picks of users
4. Keeps track of captainships of users
4. Sorts users to top10 played list
5. Sorts users to top10 last picked list
6. Sorts users to top10 first picked list
7. Allows fetching individual userstats card
8. Follows a threshold of captainship, new players don't get randomed as captain.

# Details

*Languages:*
- English
- Finnish

*Technical information:*
- Bot is running on Discore 4.2.0 (https://github.com/BundledSticksInkorperated/Discore)
- Project is running on .NET Core 2.0 https://www.microsoft.com/net/download/all
- Hosting can be run on Windows, Linux, OS X (Only Windows & OSX tested)
- See settings/appsettings.json for settings-file guidance.

*Authors:* 
- kitsun8 & pirate_patch of SuomiOW Discord (Finnish Overwatch community, https://discord.gg/tKezvfH)

*Special thanks:* 
- tleikomaa for providing a captain threshold feature!
- pirate_patch for providing Persistent Data (top lists etc.)

# Command List

- !add
- !remove / !rm
- !ready / !r
- !pick / !p
- !gatherinfo / !gi
- !gstatus / !gs
- !f10 / !fat10
- !fatkid
- !top10 / !topten
- !hs / !highscore
- !tk10
- !thinkid
- !c10
- !captain
- !resetbot
