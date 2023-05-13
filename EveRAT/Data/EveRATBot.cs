/// Author : Sébastien Duruz
/// Date : 26.04.2023

using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CoreHtmlToImage;
using ESI.NET.Models.Universe;
using EveContractsFetcher.Data;
using EveRAT.Models;
using ImageFormat = CoreHtmlToImage.ImageFormat;
using Structure = ESI.NET.Models.Sovereignty.Structure;

namespace EveRAT.Data
{
    /// <summary>
    /// Class EveRatBot
    /// </summary>
    public class EveRatBot
    {
        /// <summary>
        /// Status message for Daily DT
        /// </summary>
        private static readonly string _dailyDTStatusMessage = "Eating grass during daily DT";

        /// <summary>
        /// Status message for Working
        /// </summary>
        private static readonly string _workingStatusMessage = "Looking for kills";

        /// <summary>
        /// Message Id for the testing mode
        /// </summary>
        private ulong _messageId = 0;

        /// <summary>
        /// Discord Socket Client
        /// </summary>
        private DiscordSocketClient _client;

        /// <summary>
        /// Discord Commands Service
        /// </summary>
        private CommandService _commands;

        /// <summary>
        /// ESI Client
        /// </summary>
        private EveESI _esiClient;

        /// <summary>
        /// Settings of the bot, restart required to apply new ones
        /// </summary>
        private SettingsProvider _botSettings;

        /// <summary>
        /// Format object for currencies
        /// </summary>
        private NumberFormatInfo _localFormat;

        /// <summary>
        /// Status message of the bot
        /// </summary>
        private string _currentStatusMessage = _workingStatusMessage;

        /// <summary>
        /// Current kills data for the useful Systems
        /// </summary>
        private List<Kills> _currentKillsData;

        /// <summary>
        /// Current kills data for the useful Systems
        /// </summary>
        private List<Structure> _currentSystemsSovereigntyData;

        /// <summary>
        /// Current ADM data for the useful systems
        /// </summary>
        private List<Structure> _currentAdmData;

        /// <summary>
        /// Database Context for SQLite
        /// </summary>
        private EveRATDatabaseContext _databaseContext;

        /// <summary>
        /// Custom Constructor
        /// </summary>
        /// <param name="testing">Does the application executed as test ?</param>
        public EveRatBot()
        {
            _currentKillsData = new List<Kills>();
            _currentSystemsSovereigntyData = new List<Structure>();
            _databaseContext = new EveRATDatabaseContext();
            _botSettings = new SettingsProvider();
        }

        /// <summary>
        /// Start the bot
        /// </summary>
        /// <returns>Result of the task</returns>
        public async Task StartBot()
        {
            _esiClient = new EveESI(_botSettings.BotSettingsValues.ClientId, _botSettings.BotSettingsValues.SecretKey, _botSettings.BotSettingsValues.CallbackUrl,_botSettings.BotSettingsValues.UserAgent);
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                GatewayIntents = GatewayIntents.All,
            });

            _commands = new CommandService();
            _client.Log += ConsoleLog;
            _client.Ready += () =>
            {
                Console.WriteLine("Bot Ready");
                return Task.CompletedTask;
            };

            // set the correct Token, Testing or Production mode
            string token = _botSettings.BotSettingsValues.BotToken;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            _client.MessageReceived += HandleCommandAsync;

            Thread.Sleep(5000);

            _client.SetGameAsync(_workingStatusMessage);
            await CheckMilitaryIndex();
        }

        /// <summary>
        /// Handle user commands
        /// </summary>
        /// <param name="pMessage">message sent by users</param>
        /// <returns>Result of the task</returns>
        private async Task HandleCommandAsync(SocketMessage pMessage)
        {
            SocketUserMessage message = (SocketUserMessage)pMessage;

            if (message == null)
                return;

            // Skip if the command is not valid or not in the intended channel
            int argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos))
                return;

            SocketCommandContext context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos, null);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        /// <summary>
        /// Logging method
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <returns>Result of the Task</returns>
        public async Task ConsoleLog(LogMessage message)
        {
            Console.WriteLine($"{message}");
        }

        /// <summary>
        /// The calculations and fetch for "evaluate" the current military index lvl
        /// </summary>
        private async Task CheckMilitaryIndex()
        {
            while (true)
            {
                if (DateTime.UtcNow.TimeOfDay >= DateTime.MinValue.AddHours(10).AddMinutes(58).TimeOfDay &&
                    DateTime.UtcNow.TimeOfDay <= DateTime.MinValue.AddHours(11).AddMinutes(10).TimeOfDay)
                {
                    await _client.SetGameAsync(_dailyDTStatusMessage);
                    _currentStatusMessage = _dailyDTStatusMessage;
                    while (DateTime.UtcNow.TimeOfDay >= DateTime.MinValue.AddHours(10).AddMinutes(59).TimeOfDay &&
                           DateTime.UtcNow.TimeOfDay <= DateTime.MinValue.AddHours(11).AddMinutes(15).TimeOfDay)
                    {
                        // Wait for DT to end
                    }
                }
                else
                {
                    if (_currentStatusMessage == _dailyDTStatusMessage)
                    {
                        _currentStatusMessage = _workingStatusMessage;
                        await _client.SetGameAsync(_currentStatusMessage);
                    }

                    try
                    {
                        await FetchSystemsKills();
                        await FetchAllianceSovereignty();
                        await UpdateDatabase();

                        await BuildDailyReportMessage();
                        await RenderDailyReportImage();

                        if (_botSettings.BotSettingsValues.ActivateStats)
                        {
                            await BuildLastDaysReportMessage();
                            await RenderLastDaysReportImage();
                        }
                        
                        await UpdateSystemsMessage();
                        
                        // If old data are present
                        if(_databaseContext.Histories.Any(x => x.HistoryDateTime < DateTime.UtcNow.AddDays(-_botSettings.BotSettingsValues.DaysToKeepHistory)))
                            await ClearDatabase();

                        DateTime now = DateTime.Now;
                        while (DateTime.Now.Subtract(now).Minutes < _botSettings.BotSettingsValues.RefreshEvery)
                        {
                            // don't do anything for X minutes before restarting process
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the sytem kills for the required systems
        /// </summary>
        private async Task FetchSystemsKills()
        {
            _currentKillsData.Clear();
            List<Kills> systemKills = await _esiClient.FetchCurrentKills();

            foreach (Tuple<int, string> systemId in _botSettings.BotSettingsValues.Systems)
            {
                Kills systemData = systemKills.Where(x => x.SystemId == systemId.Item1).FirstOrDefault();

                if (systemData == null) // Not found
                    _currentKillsData.Add(new Kills()
                    {
                        SystemId = systemId.Item1,
                        NpcKills = 0,
                        PodKills = 0,
                        ShipKills = 0
                    });
                else // Found
                    _currentKillsData.Add(systemData);
            }
        }

        /// <summary>
        /// Fetch the alliance sovereignty status (ADM) for useful systems
        /// </summary>
        private async Task FetchAllianceSovereignty()
        {
            _currentSystemsSovereigntyData.Clear();
            List<Structure> systemSovereignties = await _esiClient.FetchCurrentSov();

            foreach (Tuple<int, string> systemId in _botSettings.BotSettingsValues.Systems)
            {
                Structure systemData = systemSovereignties.Where(x => x.SolarSystemId == systemId.Item1).First();
                _currentSystemsSovereigntyData.Add(systemData);
            }
        }

        /// <summary>
        /// Build the systems Message
        /// </summary>
        /// <returns>Result of the Task (the formatted message)</returns>
        private async Task BuildDailyReportMessage()
        {
            // START
            string message = @"
            <!doctype html>
            <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <title>Ratting Report</title>
                </head>
                <style>
                    body {
                        font-family: 'gg sans', 'Noto Sans', 'Helvetica Neue', Helvetica, Arial, sans-serif;
                        background: #313338;
                        color: #ffffff;
                        width: 450px;
                    }
                    table {
                        border-collapse: collapse;
                        width: 100%;
                    }
                    table, th, td{
                        border: 1px solid #707070;
                    }
                    td, th {
                        text-align: center;
                        padding-left: 2px;
                        height: 25px;
                    }
                    p {
                        font-size: small;
                    }
                    img{
                        image-rendering: pixelated;
                        image-rendering: -moz-crisp-edges;
                        width: 20px;
                        height: 20px;
                    }
                    .legends {
                        margin-top: 10px;
                        font-size: small;
                        justify-content: space-between;
                    }
                    .legendsLabel {
                        display: flex;
                        align-items: center;
                        gap: 5px;
                    }
                </style>
                <body>
                <h3>Ratting Stats (NPC Killed)</h3>
                <table>
                    <tr>
                        <th style='width: 50px'>System</th>
                        <th style='width: 36px'>ADM</th>
                        <th style='width: 40px;'>1H</th>
                        <th style='width: 40px;'>6H</th>
                        <th style='width: 40px;'>24H</th>
                        <th style='width: 34px'>Status</th>
                    </tr>";
            
            // SYSTEMS STATS
            for (int i = 0; i < _botSettings.BotSettingsValues.Systems.Count; ++i)
            {
                long sixHoursTotal = 0;
                long twentyFourHoursTotal = 0;
                string icon = "";
                
                List<History> sixHours = _databaseContext.Histories
                    .Where(x => x.HistoryDateTime >= DateTime.UtcNow.AddHours(-6) &&
                                x.HistorySystemId == _botSettings.BotSettingsValues.Systems[i].Item1).ToList();

                List<History> twentyFourHours = _databaseContext.Histories
                    .Where(x => x.HistoryDateTime >= DateTime.UtcNow.AddHours(-24) &&
                                x.HistorySystemId == _botSettings.BotSettingsValues.Systems[i].Item1).ToList();
                
                foreach (History history in sixHours)
                    sixHoursTotal += history.HistoryNpckills.Value;
                foreach (History history in twentyFourHours)
                    twentyFourHoursTotal += history.HistoryNpckills.Value;

                // ICON
                if (twentyFourHoursTotal > _botSettings.BotSettingsValues.Limits[1]) // All good !
                    icon = "<img src='data:image/png;base64,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' alt='vu'/>";
                else if (twentyFourHoursTotal > _botSettings.BotSettingsValues.Limits[0]) // A bit more ! 
                    icon = "<img src='data:image/png;base64,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' alt='warning'/>";
                else // Need a lot !
                    icon = "<img src='data:image/png;base64,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' alt='crab'/>";
                
                message += @$"
                <tr>
                    <td>{_botSettings.BotSettingsValues.Systems[i].Item2}</td>
                    <td>{_currentSystemsSovereigntyData.First(x => x.SolarSystemId == _botSettings.BotSettingsValues.Systems[i].Item1).VulnerabilityOccupancyLevel.ToString()}</td>
                    <td>{_currentKillsData.First(x => x.SystemId == _botSettings.BotSettingsValues.Systems[i].Item1).NpcKills.ToString()}</td>
                    <td>{sixHoursTotal.ToString()}</td>
                    <td>{twentyFourHoursTotal.ToString()}</td>
                    <td style='text-align: center'>
                        {icon}
                    </td>
                </tr>";
            }

            // END
            message += @"
                    </table>
                    <div class='legends'>
                        <div class='legendsLabel'>
                            <img src='data:image/png;base64,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' alt='crab'/>&ensp;Krabbers Please ! &ensp;
                            <img src='data:image/png;base64,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' alt='warning'/>&ensp;Almost done ! &ensp;
                            <img src='data:image/png;base64,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' alt='vu'/>&ensp;All good for today !
                        </div>
                    </div>
                    <p>Developed by <span style='font-weight: bold;'>github.com/SebastienDuruz</span></p>
                </body>
                </html>";

            await File.WriteAllTextAsync("discordMessage.html", message);
        }

        private async Task BuildLastDaysReportMessage()
        {
            if (_botSettings.BotSettingsValues.ActivateStats)
            {
                // START
                string message = @"<!doctype html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <title>Ratting Report</title>
                    </head>
                    <style>
                        body {
                            font-family: 'gg sans', 'Noto Sans', 'Helvetica Neue', Helvetica, Arial, sans-serif;
                            background: #313338;
                            color: #ffffff;
                            width: 520px;
                        }
                        table {
                            border-collapse: collapse;
                            width: 100%;
                        }
                        table, th, td{
                            border: 1px solid #707070;
                        }
                        td, th {
                            text-align: center;
                            padding-left: 2px;
                            height: 25px;
                        }
                        p {
                            font-size: small;
                        }
                        img{
                            width: 20px;
                            height: 20px;
                        }
                        .legends {
                            margin-top: 10px;
                            display: flex;
                            font-size: small;
                            justify-content: space-between;
                        }
                        .legendsLabel {
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            gap: 5px;
                        }
                    </style>
                    <body>
                        <h3>Last 7 days report</h3>
                        <table>
                            <tr>
                                <th style='width: 50px'>System</th>";

                // HEADER
                for (int l = 1; l < 8; ++l)
                {
                    message +=
                        $@"<th style='width: 40px'>{string.Concat(DateTime.UtcNow.AddDays(-l).Day.ToString("D2"), ".", DateTime.UtcNow.AddDays(-l).Month.ToString("D2"))}</th>";
                }
                message += @"</tr>";

                // DATA
                for (int j = 0; j < _botSettings.BotSettingsValues.Systems.Count; ++j)
                {
                    List<long> lastSevenDaysTotal = new List<long>()
                    {
                        0, 0, 0, 0, 0, 0, 0
                    };

                    // Fetch the last 7 days stats
                    List<History> firstDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-1).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> secondDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-2).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> thirdDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-3).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> fourthDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-4).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> fifthDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-5).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> sixthDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-6).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();
                    List<History> seventhDay = _databaseContext.Histories.Where(x =>
                        x.HistoryDateTime.Day == DateTime.UtcNow.AddDays(-7).Day &&
                        x.HistorySystemId == _botSettings.BotSettingsValues.Systems[j].Item1).ToList();

                    // Calculate the total foreach days (-1 to -7)
                    foreach (History history in firstDay)
                        lastSevenDaysTotal[0] += history.HistoryNpckills.Value;
                    foreach (History history in secondDay)
                        lastSevenDaysTotal[1] += history.HistoryNpckills.Value;
                    foreach (History history in thirdDay)
                        lastSevenDaysTotal[2] += history.HistoryNpckills.Value;
                    foreach (History history in fourthDay)
                        lastSevenDaysTotal[3] += history.HistoryNpckills.Value;
                    foreach (History history in fifthDay)
                        lastSevenDaysTotal[4] += history.HistoryNpckills.Value;
                    foreach (History history in sixthDay)
                        lastSevenDaysTotal[5] += history.HistoryNpckills.Value;
                    foreach (History history in seventhDay)
                        lastSevenDaysTotal[6] += history.HistoryNpckills.Value;

                    message += $"<tr><td>{_botSettings.BotSettingsValues.Systems[j].Item2}</td>";

                    for (int k = 0; k < lastSevenDaysTotal.Count(); ++k)
                    {
                        if (lastSevenDaysTotal[k] > _botSettings.BotSettingsValues.Limits[1])
                            message += @"<td style='text-align: center'><img src='data:image/png;base64,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' alt='vu'/></td>";
                        else if (lastSevenDaysTotal[k] > _botSettings.BotSettingsValues.Limits[0])
                            message += @"<td style='text-align: center'><img src='data:image/png;base64,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' alt='warning'/></td>";
                        else
                            message += @"<td style='text-align: center'>
                                        <img src='data:image/png;base64,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' alt='crab'/>
                                        </td>";
                    }

                    // END
                    message += @"</tr>";
                }

                message += @"</table></body></html>";
                await File.WriteAllTextAsync("lastDaysMessage.html", message);
            }
        }

        /// <summary>
        /// Render the daily report image
        /// </summary>
        private async Task RenderDailyReportImage()
        {
            var converter = new HtmlConverter();
            var html = await File.ReadAllTextAsync("discordMessage.html");
            var bytes = converter.FromHtmlString(html, width: 460, ImageFormat.Png);
            await File.WriteAllBytesAsync("discordMessage.png", bytes);
        }
        
        /// <summary>
        /// Render the daily report image
        /// </summary>
        private async Task RenderLastDaysReportImage()
        {
            var converter = new HtmlConverter();
            var html = await File.ReadAllTextAsync("lastDaysMessage.html");
            var bytes = converter.FromHtmlString(html, width: 550, ImageFormat.Png);
            await File.WriteAllBytesAsync("lastDaysMessage.png", bytes);
        }
        
        /// <summary>
        /// Update the Message into Discord channel
        /// </summary>
        /// <param name="messageContent"></param>
        private async Task UpdateSystemsMessage()
        {
            await _client.GetGuild(_botSettings.BotSettingsValues.DiscordServerId)
                .GetTextChannel(_botSettings.BotSettingsValues.DiscordChannelId)
                .DeleteMessagesAsync(await _client.GetGuild(_botSettings.BotSettingsValues.DiscordServerId)
                    .GetTextChannel(_botSettings.BotSettingsValues.DiscordChannelId).GetMessagesAsync().FlattenAsync());
            
            await _client.GetGuild(_botSettings.BotSettingsValues.DiscordServerId)
                .GetTextChannel(_botSettings.BotSettingsValues.DiscordChannelId)
                .SendFileAsync("discordMessage.png", "");

            if (_botSettings.BotSettingsValues.ActivateStats)
                await _client.GetGuild(_botSettings.BotSettingsValues.DiscordServerId)
                    .GetTextChannel(_botSettings.BotSettingsValues.DiscordChannelId)
                    .SendFileAsync("lastDaysMessage.png", "");
        }

        /// <summary>
        /// Update the database if new datas has ben returned by the ESI
        /// </summary>
        /// <returns>True if an update has been executed, false if not</returns>
        private async Task<bool> UpdateDatabase()
        {
            // Nothing pushed at the moment : Push the new data
            if (!_databaseContext.Histories.ToList()
                    .Exists(x => x.HistoryDateTime >= _esiClient.CurrentLastModified))
            {
                for (int i = 0; i < _botSettings.BotSettingsValues.Systems.Count; ++i)
                {
                    History history = new History()
                    {
                        HistoryNpckills = _currentKillsData.First(x => x.SystemId == _botSettings.BotSettingsValues.Systems[i].Item1)
                            .NpcKills,
                        HistorySystemId = _botSettings.BotSettingsValues.Systems[i].Item1,
                        HistoryDateTime = _esiClient.CurrentLastModified.Value.AddMinutes(5),
                        HistoryAdm = _currentSystemsSovereigntyData
                            .First(x => x.SolarSystemId == _botSettings.BotSettingsValues.Systems[i].Item1).VulnerabilityOccupancyLevel
                    };

                    await _databaseContext.Histories.AddAsync(history);
                    await _databaseContext.SaveChangesAsync();
                }

                return true;
            }
            // Already saved on DB, do nothing !
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clear the database data that is older than X days
        /// </summary>
        private async Task ClearDatabase()
        {
            List<History> oldHistories = _databaseContext.Histories
                .Where(x => x.HistoryDateTime < DateTime.UtcNow.AddDays(-_botSettings.BotSettingsValues.DaysToKeepHistory)).ToList();
            
            _databaseContext.RemoveRange(oldHistories);
            await _databaseContext.SaveChangesAsync();
        }
    }
}