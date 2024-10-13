using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Utility;
using Google.Apis.Sheets.v4.Data;
using MiqoteaRoomOrderManager.Helpers;
using MiqoteaRoomOrderManager.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using static MiqoteaRoomOrderManager.Windows.ConfigWindow;

namespace MiqoteaRoomOrderManager
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        // Player
        public Player player;
        public bool shitStarted { get; set; } = false;
        // Food List
        public List<List<Food>> foodList = new();
        public bool IsConfigWindowMovable { get; set; } = true;
        public List<string> TypeOrder = new List<string> { "starter", "main", "dessert", "drink" };
        public uint currentGil { get; set; } = 0;
        public uint totalReceived { get; set; } = 0;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }

    public class Food
    {
        public int Price;
        public int Amount;
        public string Name;
        public int MenuItemId;
        public int BaseFoodAmount;

        public Food(int price, int amount, string name, int menuItemId, int baseFoodAmount)
        {
            Price = price;
            Amount = amount;
            Name = name;
            MenuItemId = menuItemId;
            BaseFoodAmount = baseFoodAmount;
        }
    }

    public class MenuItemResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class MenuResponse
    {
        public required MenuItemResponse[] MenuItems { get; set; }
    }
}
