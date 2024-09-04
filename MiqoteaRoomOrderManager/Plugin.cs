using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using MiqoteaRoomOrderManager.Windows;
using Dalamud.Game.ClientState;
using Dalamud.Plugin.Services;

namespace MiqoteaRoomOrderManager
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Miqo'tea Room Order Manager";
        private const string CommandName = "/mroom";

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("MiqoteaRoomOrderManager");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin()
        {

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // you might normally want to embed resources and load them from the manifest stream
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
        }

        public void Dispose()
        {
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

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI() => ConfigWindow.Toggle();

        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
