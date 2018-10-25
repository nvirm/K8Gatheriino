# K8Gatheriino

![alt text](https://github.com/kitsun8/K8Gatheriino/blob/master/screenshots/gatheriino3.PNG)


A bot for Discord that helps people to form pickup games (PUGs).
Originally for Overwatch, but as the team size can be changed from settings, works with any team based game.

# What it does

0. Reads settings from appsettings.json (sample found in 'settings' in this repo)
1. Gathers X amount of people to a queue
2. Asks players to Ready up inside Y amount of time.
3. Kicks unreadied players out of the queue.
4. Decides 2 random team captains.
4a. Allows Randomed captain to pass captainship to someone with more experience
5. Allows captains to pick X amount of players to teams, 1 by 1.
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

Bot is running on Discore 4.2.0 (https://github.com/BundledSticksInkorperated/Discore)

Project is running on .NET Core 2.0 https://www.microsoft.com/net/download/all

Authors: kitsun8 & pirate_patch of SuomiOW Discord (Finnish Overwatch community, https://discord.gg/tKezvfH)
Special thanks: tleikomaa for providing a captain threshold feature!

Language support added for english, default/original language was finnish. Can be changed from appsettings.json
Thanks to hpr's additions, the bot also keeps track of players' statistics in separate CSV files. If the files are nonexistent, they will be created.
