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
using System.Xml.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
using static MiqoteaRoomOrderManager.Windows.MainWindow;

namespace MiqoteaRoomOrderManager.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin Plugin;
    public string CodeInput = "";
    public string Code = "";
    public int PlayerIdx = -1;
    public string PlayerName = "";
    public string Password = "";
    public bool isloading = false;
    public bool Error = false;
    private readonly string[] allowedPlayers = ["Noftasmos Moon", "Vyreia Sun:", "Sage Loxley", "Ra'ish Sooyin"];
    public string? selectedPlayer = null;
    public string[] staffNames = [];

    public ConfigWindow(Plugin plugin) : base(
        "Miqo'tea Room Order Manager Config",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(232, 75),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Plugin = plugin;
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
        if (isloading)
        {
            ImGui.Text($"Loading...");
            return;
        }
        if (Plugin.Configuration.player != null && Array.IndexOf(allowedPlayers, Plugin.Configuration.player.Name) != -1 && staffNames.Length > 0)
        {
            ImGui.Begin("Staff List");
            ImGui.BeginChild("", new Vector2(200, 400), true);
            foreach (var s in staffNames)
            {
                var isSelected = selectedPlayer != null && selectedPlayer.Equals(s);

                if (ImGui.Selectable(s, isSelected))
                {
                    selectedPlayer = s;
                }
            }

            ImGui.EndChild();
            ImGui.BeginDisabled(selectedPlayer == null);
            if (ImGui.Button("Mark their presence"))
            {
                Plugin.apiClient.PostAsync<MarkStaffPresenceRequest, MarkStaffPresenceResponse>($"api/v1/staff/mark-presence", new MarkStaffPresenceRequest(selectedPlayer)).ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        selectedPlayer = null;
                    }
                });

            }
            ImGui.EndDisabled();
            ImGui.End();
        }
        if(Plugin.Configuration.player != null && Array.IndexOf(allowedPlayers, Plugin.Configuration.player.Name) != -1){
            _ = Plugin.apiClient.GetAsync<StaffResponse>(endpoint: "/api/v1/staff").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var response = task.GetResultSafely();

                    var staff = response.staff;
                    staffNames = staff.Select(s => s.UserName).ToArray();
                }
            });
        }
        ImGui.Text($"Current linked player:");
        ImGui.SameLine();
        if (Plugin.Configuration.player == null)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "linking has not been complete");
        }
        else
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{Plugin.Configuration.player.Name}");
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        if (Error)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Your current character is not present in the database.");
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Please try with the character that you registered in the admin panel.");
        }
        if(Plugin.Configuration.player == null)
        {
            ImGui.Text("Password");
            ImGui.InputText("", ref Password, (uint)20, ImGuiInputTextFlags.Password);
            if (ImGui.Button("Link player with plugin"))
            {
                isloading = true;
                PlayerName = Plugin.ClientState.LocalPlayer?.Name.ToString();
                var requestBody = new PlayerRequest(PlayerName, Password);
                    _ = Plugin.apiClient.PostAsync<PlayerRequest, PlayerResponse>(endpoint: "/api/v1/login", content: requestBody).ContinueWith(task =>
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
                                Plugin.Configuration.player = new Player(PlayerName, token);
                                Plugin.apiClient.SetAuthorizationHeader(token);
                                _ = Plugin.apiClient.GetAsync<ShiftResponse>(endpoint: "/api/v1/shifts/latest").ContinueWith(task =>
                                {
                                    if (task.IsCompletedSuccessfully)
                                    {
                                        var response = task.GetResultSafely();

                                        if (response.IsActive) {
                                            Plugin.LoadMenu();
                                            Plugin.Configuration.shitStarted = true;
                                            Plugin.Configuration.currentGil = Plugin.GetGilCount();
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
        if (Plugin.Configuration.player != null)
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

    public class StaffType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("discordUsername")]
        public string DiscordUsername { get; set; }

        [JsonPropertyName("discordId")]
        public string DiscordId { get; set; }

        [JsonPropertyName("miqoCredits")]
        public int MiqoCredits { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class StaffResponse
    {
        [JsonPropertyName("staff")]
        public StaffType[] staff { get; set; }
    }

    public class MarkStaffPresenceRequest(string name)
    {
        public string name { get; set; } = name;
    }

    public class MarkStaffPresenceResponse
    {
        [JsonPropertyName("staff")]
        public StaffType staff { get; set; }
    }

}
