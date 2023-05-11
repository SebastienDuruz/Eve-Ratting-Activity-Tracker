# Eve-Ratting-Activity-Tracker
A simple discord bot that reports the recent ratting activity on a given list of NS system.

The information are extracted from the public data available on [Eve ESI](https://esi.evetech.net/ui/).

# Exemple Output
The base module looks like that :

![Exemple1.png](Screenshots%2FExemple1.png)

If you activate stats (*"ActivateStats": **true***) a second message is generated :

![Exemple2.png](Screenshots%2FExemple2.png)

## Compatibility

- Linux **x64 / Arm64**
- Windows **10 / 11**

## Configuration file
```
{
  "BotToken": "Discord_bot_token",
  "ClientId": "Eve_app_clientId",
  "SecretKey": "Eve_app_securityKey",
  "CallbackUrl": "Eve_app_callbackUrl",
  "UserAgent": "Eve_app_userAgent",
  "DiscordServerId": 0,
  "DiscordChannelId": 0,
  "Limits": [
    300,
    500
  ],
  "RefreshEvery": 5,
  "DaysToKeepHistory": 20,
  "ActivateStats": false,
  "Systems": [
    {
      "Item1": 30002172,
      "Item2": "W4E-IT"
    },
    {
      "Item1": 30002173,
      "Item2": "OP9L-F"
    }
  ]
}
```

## Installation
Get the last release for your targeted architecture.

### Config file
Setup the **config file** with the following information :
#### Discord Bot
https://discord.com/developers/applications
```
BotToken            -> Token of your Discord Bot
```
#### ESI Access
https://developers.eveonline.com/applications
```
ClientId            -> ClientId of your Eve Application
SecretKey           -> SecretKey of your Eve Application
CallbackUrl         -> CallbackURL of your Eve Application
UserAgent           -> UserAgent used to identify your calls to CCP (I usually put my main character name)
```
#### General Settings
```
DiscordServerId     -> ServerId of your Discord Server
DiscordChannelId    -> ChannelId used by the bot to push messages (channel will be automaticaly purged every time)
Limits              -> Limits of the "ratting level" (Krabbers please ! <--> Almost done ! <--> All good for today !)
RefreshEvery        -> Refresh the process after X minutes
DaysToKeepHistory   -> Number of days to keep the history into the SQLite database
ActivateStats       -> Activate/Desactivate the "Stats"
Systems             -> The system(s) to track (you can add as many as you want) [Item1 = System Id] [Item2 = System Label]
```

### Dependencies
For **Linux** machines make sure **wkhtmltoimage** is installed :

Ubuntu : `sudo apt install wkhtmltoimage`

Fedora : `sudo dnf install wkhtmltoimage`

## Licence
This software is open-sourced software licensed under [GNU General Public License v3.0](LICENSE)
