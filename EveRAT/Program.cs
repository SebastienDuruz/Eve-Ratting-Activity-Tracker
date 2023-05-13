/// Author : Sébastien Duruz
/// Date : 26.04.2023

using EveRAT.Data;

/// <summary>
/// Main Entry of the program
/// </summary>
public class Program
{
    /// <summary>
    /// DiscordBot main instance
    /// </summary>
    EveRatBot DiscordBot { get; set; }

    /// <summary>
    /// Startup of the bot
    /// </summary>
    /// <param name="args">Args</param>
    /// <returns>Result of the Task</returns>
    public static Task Main(string[] args) => new Program().RunAsync();

    /// <summary>
    /// Setup and Start the bot
    /// </summary>
    /// <param name="host">Host of the bot</param>
    /// <returns>Result of the task</returns>
    public async Task RunAsync()
    {
        DiscordBot = new EveRatBot();
        await DiscordBot.StartBot();

        await Task.Delay(-1);
    }
}