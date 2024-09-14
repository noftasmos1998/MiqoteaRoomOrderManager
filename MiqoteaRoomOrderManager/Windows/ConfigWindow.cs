using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Google.Apis.Sheets.v4;
using ImGuiNET;
using MiqoteaRoomOrderManager.Helpers;
using static MiqoteaRoomOrderManager.Windows.ConfigWindow;

namespace MiqoteaRoomOrderManager.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin plugin;
    public string CodeInput = "";
    public string Code = "";
    public int PlayerIdx = -1;
    public string PlayerName = "";
    public string Password = "";
    public bool isloading = false;
    public bool Error = false;
    public readonly MiqoteaAPIHelper apiClient = new MiqoteaAPIHelper();

    public ConfigWindow(Plugin plugin) : base(
        "Miqo'tea Room Order Manager Config",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(232, 75),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;

    }

    public void Dispose() { }

    public static string ExcelColumnFromNumber(int column)
    {
        string columnString = "";
        decimal columnNumber = column;
        while (columnNumber > 0)
        {
            decimal currentLetterNumber = (columnNumber - 1) % 26;
            char currentLetter = (char)(currentLetterNumber + 65);
            columnString = currentLetter + columnString;
            columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
        }
        return columnString;
    }

    public override void Draw()
    {
        //Plugin.DrawTablesWindow();
        if(isloading)
        {
            ImGui.Text($"Loading...");
            return;
        }
        if(plugin.Configuration.player != null && plugin.Configuration.player.Name == "Noftasmos Moon"){
            ImGui.Text($"{plugin.Configuration.totalReceived}");
            ImGui.Text($"{plugin.Configuration.currentGil}");
        }
        ImGui.Text($"Current linked player:");
        ImGui.SameLine();
        if (plugin.Configuration.player == null)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "linking has not been complete");
        }
        else
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{plugin.Configuration.player.Name}");
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        if (Error)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Your current character is not present in the database.");
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Please try with the character that you registered in the admin panel.");
        }
        if(plugin.Configuration.player == null)
        {
            ImGui.Text("Password");
            ImGui.InputText("", ref Password, (uint)20, ImGuiInputTextFlags.Password);
            if (ImGui.Button("Link player with plugin"))
            {
                isloading = true;
                PlayerName = Plugin.ClientState.LocalPlayer?.Name.ToString();
                var requestBody = new PlayerRequest(PlayerName, Password);
                    _ = apiClient.PostAsync<PlayerRequest, PlayerResponse>(endpoint: "/api/v1/login", content: requestBody).ContinueWith(task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            var (statusCode, response) = task.GetResultSafely();

                            if (statusCode == HttpStatusCode.Unauthorized)
                            {
                                isloading = false;
                                Error = true;
                            }
                            else
                            {
                                PlayerName = Plugin.ClientState.LocalPlayer?.Name.ToString();
                                string token = response.Token;
                                // Create the player object after the HTTP request
                                plugin.Configuration.player = new Player(PlayerName, token);
                                apiClient.SetAuthorizationHeader(token);
                                _ = apiClient.GetAsync<ShiftResponse>(endpoint: "/api/v1/shifts/latest").ContinueWith(task =>
                                {
                                    if (task.IsCompletedSuccessfully)
                                    {
                                        var response = task.GetResultSafely();

                                        if(response.IsActive) {
                                            plugin.Configuration.LoadMenu();
                                            plugin.Configuration.shitStarted = true;
                                            plugin.Configuration.currentGil = plugin.GetGilCount();
                                        }
                                        isloading = false;
                                    }
                                });
                            }
                        }
                        else
                        {
                            // Handle the case when the task fails
                            Console.WriteLine("Failed to link player with the plugin. Task failed.");
                        }
                    });
            }
        }
        if (plugin.Configuration.player != null)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Linked succesfully");
            ImGui.Spacing();
        }
    }

    public class Player(string name, string token)
    {
        public string Name { get; set; } = name;
        public string Token { get; set; } = token;
    }

    public class UserResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("discordUsername")]
        public string DiscordUsername { get; set; }

        [JsonPropertyName("discordId")]
        public string DiscordId { get; set; }

        [JsonPropertyName("miqoCredits")]
        public int MiqoCredits { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class PlayerResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("user")]
        public UserResponse User { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
    public class PlayerRequest(string email, string password)
    {
        public string email { get; set; } = email;
        public string password { get; set; } = password;
    }

    public class ShiftResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("canceled")]
        public bool Canceled { get; set; }
        [JsonPropertyName("isFinished")]
        public bool IsFinished { get; set; }
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

}
