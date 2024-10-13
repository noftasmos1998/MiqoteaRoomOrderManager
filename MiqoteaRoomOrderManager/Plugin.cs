using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using MiqoteaRoomOrderManager.Windows;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using MiqoteaRoomOrderManager.Helpers;
using Dalamud.Utility;
using System.Collections.Generic;
using System.Linq;

namespace MiqoteaRoomOrderManager
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Miqo'tea Room Order Manager";
        private const string CommandName = "/mroom";

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("MiqoteaRoomOrderManager");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }
        private InventoryManager InventoryManager { get; init; }
        public readonly MiqoteaAPIHelper apiClient = new();

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            var logoPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "logo.png");

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, logoPath);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Order manager window"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            // This adds a button to the plugin installer entry of this plugin which allows
            // to toggle the display status of the configuration ui
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Adds another button that is doing the same but for the main ui of the plugin
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            ChatGui.ChatMessage += MainWindow.OnChatMessage;
        }

        public void Dispose()
        {
            ChatGui.ChatMessage -= MainWindow.OnChatMessage;
            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            switch (command)
            {
                case CommandName:
                    ToggleMainUI();
                    break;
            }
        }

        public void LoadMenu()
        {
            apiClient.GetAsync<MenuResponse>("/api/v1/menus/current").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var response = task.GetResultSafely();

                    var menuItems = response.MenuItems;

                    for (var i = 0; i < Configuration.TypeOrder.Count; i++)
                    {   
                        Configuration.foodList.Add([]);
                        foreach (var item in menuItems)
                        {
                            if (item.Type == Configuration.TypeOrder[i])
                            {
                                Configuration.foodList[i].Add(new Food(item.Price, item.Quantity, item.Name, item.Id, item.Quantity));
                            }
                        }
                    }

                }
                else
                {
                    // Handle the case when the task fails
                    Console.WriteLine("Failed to link player with the plugin. Task failed.");
                }
            });
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI() => ConfigWindow.Toggle();

        public void ToggleMainUI() => MainWindow.Toggle();

        public List<IPlayerCharacter> GetNearbyPlayers()
        {
            List<IPlayerCharacter> players = [];
            // Iterate over the objects in the ObjectTable
            for (var i = 0; i < ObjectTable.Length; i++)
            {
                var gameObject = ObjectTable[i];

                // Check if the object is a PlayerCharacter
                if (gameObject is IPlayerCharacter player)
                {
                    players.Add(player);
                }
            }
            return players;
        }

        unsafe public uint GetGilCount()
        {
            var invManager = InventoryManager.Instance();

            return invManager->GetGil();
        }
    }
}
