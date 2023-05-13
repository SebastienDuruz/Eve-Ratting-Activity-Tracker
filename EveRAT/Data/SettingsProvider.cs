/// Author : Sébastien Duruz
/// Date : 03.05.2023

using System;
using System.IO;
using EveRAT.Models;
using Newtonsoft.Json;

namespace EveRAT.Data;

/// <summary>
/// Class SettingsProvider
/// </summary>
public class SettingsProvider
{
    /// <summary>
    /// File path of the userSettings file
    /// </summary>
    private string FilePath { get; }

    /// <summary>
    /// Objects that contains the settings values
    /// </summary>
    public BotSettings BotSettingsValues { get; set; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public SettingsProvider()
    {
        FilePath = "botSettings.json";
        
        ReadUserSettings();
    }

    /// <summary>
    /// Read the userSettings json file, create it if not exists
    /// Load the content into UserSettings Object
    /// </summary>
    public void ReadUserSettings()
    {
        if(File.Exists(FilePath))
        {
            try
            {
                BotSettingsValues = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(FilePath));
            }
            catch (Exception ex)
            {
                // Reset the settings by recreating a file
                WriteUserSettings();
                BotSettingsValues = new BotSettings();
            }
        }
        else
        {
            BotSettingsValues = new BotSettings();
            WriteUserSettings();
        }
    }

    /// <summary>
    /// Write the UserSettings object to json file
    /// </summary>
    public void WriteUserSettings()
    {
        try
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(BotSettingsValues, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}