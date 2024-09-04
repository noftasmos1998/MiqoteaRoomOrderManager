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
        public readonly MiqoteaAPIHelper apiClient = new();
        public bool IsConfigWindowMovable { get; set; } = true;
        public List<string> TypeOrder = new List<string> { "starter", "main", "dessert", "drink" };

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public void LoadMenu()
        {
            apiClient.SetAuthorizationHeader(player.Token);
            apiClient.GetAsync<MenuResponse>("/api/v1/menus/current").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var response = task.GetResultSafely();

                    var menuItems = response.MenuItems;

                    for (var i = 0; i < TypeOrder.Count; i++)
                    {
                        foodList.Add([]);
                        foreach (var item in menuItems)
                        {
                            if(item.Type == TypeOrder[i])
                            {
                                foodList[i].Add(new Food(item.Price, item.Quantity, item.Name, item.Id));
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
    }

    public class Food
    {
        public int Price;
        public int Amount;
        public string Name;
        public int MenuItemId;

        public Food(int price, int amount, string name, int menuItemId)
        {
            Price = price;
            Amount = amount;
            Name = name;
            MenuItemId = menuItemId;
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
