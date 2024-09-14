using System;
using System.Data.Common;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.IO;
using static MiqoteaRoomOrderManager.Windows.ConfigWindow;
using System.Text.Json.Serialization;

namespace MiqoteaRoomOrderManager.Windows;

public class MainWindow : Window, IDisposable
{
    private string LogoPath;
    private Plugin Plugin;
    static List<Order> OrderList = new();
    static List<List<Food>> ListOfFoods = new();
    static List<string> ListOfFoodsName = new(new string[] { "Light Bites", "Platters", "Desserts", "Drinks" });
    public static int Tip = 0;

    public class OrderContentResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OrderItem(int menuItemId, int quantity)
    {
        [JsonPropertyName("menuItemId")]
        public int menuItemId { get; set; } = menuItemId;

        [JsonPropertyName("quantity")]
        public int quantity { get; set; } = quantity;
    }

    public class OrderResponse
    {
        [JsonPropertyName("order")]
        public OrderContentResponse Order { get; set; }
    }
    public class OrderRequest(int total,uint totalRecieved, OrderItem[] orderItems)
    {
        [JsonPropertyName("total")]
        public int total { get; set; } = total;
        [JsonPropertyName("totalRecieved")]
        public uint totalRecieved { get; set; } = totalRecieved;

        [JsonPropertyName("orderItems")]
        public OrderItem[] orderItems { get; set; } = orderItems;
    }

    public MainWindow(Plugin plugin, string logoPath) : base(
        "Miqo'tea Room Order Manager" +
        "", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse )
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(690, 910),
            MaximumSize = new Vector2(690, float.MaxValue)
        };

        LogoPath = logoPath;
        Plugin = plugin;
        ListOfFoods = Plugin.Configuration.foodList;

        OrderList.Add(new Order(Plugin, $"New Order {OrderList.Count}"));

    }

    public override void Draw()
    {
        var logoImage = Plugin.TextureProvider.GetFromFile(LogoPath).GetWrapOrDefault();
        ImGui.Indent(280);
        ImGui.Image(logoImage.ImGuiHandle, new Vector2(120,120));
        ImGui.Unindent(280);
        ImGui.Indent(235);
        ImGui.Spacing();
        ImGui.SetWindowFontScale(1.2f);
        ImGui.Text("Miqo'tea Room Order Manager");
        ImGui.SetWindowFontScale(1f);
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Unindent(235);
        if (Plugin.Configuration.player != null)
        {
            ImGui.SameLine(ImGui.GetWindowWidth() - 160);
            ImGui.Text("Player: ");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{Plugin.Configuration.player.Name}");
            ImGui.Spacing();
            ImGui.Spacing();
            if (!Plugin.Configuration.shitStarted)
            {
                ImGui.Indent(263);
                ImGui.Spacing();
                ImGui.SetWindowFontScale(1.2f);
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Shift hasn't started yet!");
                ImGui.SetWindowFontScale(1f);
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Unindent(263);
            }
            else
            {
                if (ImGui.BeginTabBar("Orders Tab Bar", ImGuiTabBarFlags.None))
                {
                    if (ImGui.TabItemButton("+", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
                    {
                        OrderList.Add(new Order(Plugin, $"New Order {OrderList.Count}"));
                    }

                    for (var n = 0; n < OrderList.Count; n++)
                    {
                        if (ImGui.BeginTabItem(OrderList[n].OrderName))
                        {
                            OrderList[n].DrawOrderContent(n);
                        }
                    }
                    ImGui.EndTabBar();
                }
            }
        } else
        {
            ImGui.Indent(120);
            ImGui.Spacing();
            ImGui.SetWindowFontScale(1.2f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Please link your character with the plugin via the config window ");
            ImGui.SetWindowFontScale(1f);
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Unindent(120);
        }
    }

    public void Dispose() { }

    public class Order
    {
        private Plugin Plugin;
        public int GrandTotal = 0;
        public List<Food> FoodList = new();
        public List<int> QuantityFoodList = new(new int[] { 1, 1, 1, 1 });
        public string OrderName = "";
        public int foodIdxPlatters = 0;
        public int foodIdxLightBites = 0;
        public int foodIdxDesserts = 0;
        public int foodIdxDrinks = 0;
        public int foodQuantityPlatters = 1;
        public int foodQuantityLightBites = 1;
        public int foodQuantityDesserts = 1;
        public int foodQuantityDrinks = 1;
        public string changeOrderName = "";

        public Order(Plugin plugin, string orderName) 
        {
            Plugin = plugin;
            OrderName = orderName;
        }

        public void DrawOrderContent(int index)
        {

            ImGui.Spacing();
            ImGui.Indent(280);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text("Grand Total:");
            ImGui.SameLine(0.0f, 5);
            ImGui.TextColored(new Vector4(0, 255, 0, 1), $"{GrandTotal}");
            ImGui.SetWindowFontScale(1f);
            ImGui.Unindent(280);
            ImGui.Spacing();
            ImGui.PushItemWidth(150);
            ImGui.InputText($"##{OrderName}", ref changeOrderName, (uint)20);
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.36f, 0.32f, 1));
            if (ImGui.Button($"Change Order Name##{OrderName}"))
            {
                OrderName = changeOrderName;
                changeOrderName = "";
            }
            ImGui.PopStyleColor();
            ImGui.Spacing();
            FoodContent();
            ImGui.Spacing();
            if (ImGui.BeginTable($"##{OrderName}", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInner))
            {
                for (var row = 0; row < FoodList.Count; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{FoodList[row].Name} x ");
                    ImGui.SameLine(0, 0);
                    ImGui.TextColored(new Vector4(0, 255, 0, 1), $"{FoodList[row].Amount}");
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0.05f));
                    ImGui.SameLine(ImGui.GetWindowWidth() - 40);
                    if (ImGui.Button($"X##{row}", new Vector2(30f, 20f)))
                    {
                        GrandTotal -= FoodList[row].Price * FoodList[row].Amount;
                        FoodList.RemoveAt(row);
                    }
                    ImGui.PopStyleColor();
                }
                ImGui.EndTable();
            }
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.65f, 0.02f, 1));
            if (ImGui.Button("Finish Order", new Vector2(100f, 25f)))
            {
                var requestBody = new OrderRequest(GrandTotal,Plugin.Configuration.totalReceived, FoodList.Select(food => new OrderItem(food.MenuItemId, food.Amount)).ToArray());
                Plugin.Configuration.apiClient.PostAsync<OrderRequest, OrderResponse>("/api/v1/orders", requestBody).ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        OrderList.RemoveAt(index);
                        Plugin.Configuration.totalReceived = 0;
                    }
                });
                
            }
            ImGui.PopStyleColor();
            ImGui.EndTabItem();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0.65f, 0, 1));
            if (ImGui.Button("Reset Order", new Vector2(100f, 25f)))
            {
                GrandTotal = 0;
                foodIdxLightBites = foodIdxDrinks = foodIdxDesserts = foodIdxPlatters = 0;
                QuantityFoodList = new(new int[] { 1, 1, 1, 1 });
                FoodList.Clear();
            }
            ImGui.PopStyleColor();
        }

        public void FoodContent()
        {
            for (var i = 0; i < ListOfFoods.Count; i++)
            {
                var foods = ListOfFoods[i];
                var idx = 0;
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.19f, 0.80f, 0.19f, 1), $"{ListOfFoodsName[i]}");        
                switch (ListOfFoodsName[i])
                {
                    case "Light Bites":
                        ImGui.Combo($"##{ListOfFoodsName[i]}", ref foodIdxLightBites, foods.Select(food => food.Name).ToArray(), foods.Count);
                        idx = foodIdxLightBites;
                        break;
                    case "Platters":
                        ImGui.Combo($"##{ListOfFoodsName[i]}", ref foodIdxPlatters, foods.Select(food => food.Name).ToArray(), foods.Count);
                        idx = foodIdxPlatters;
                        break;
                    case "Desserts":
                        ImGui.Combo($"##{ListOfFoodsName[i]}", ref foodIdxDesserts, foods.Select(food => food.Name).ToArray(), foods.Count);
                        idx = foodIdxDesserts;
                        break;
                    case "Drinks":
                        ImGui.Combo($"##{ListOfFoodsName[i]}", ref foodIdxDrinks, foods.Select(food => food.Name).ToArray(), foods.Count);
                        idx = foodIdxDrinks;
                        break;
                }
                ImGui.SameLine();
                ImGui.PushButtonRepeat(true);
                if (ImGui.ArrowButton($"##left{i}", ImGuiDir.Left)) { --QuantityFoodList[i]; }
                ImGui.SameLine(0.0f, 10);
                if (ImGui.ArrowButton($"##right{i}", ImGuiDir.Right)) { ++QuantityFoodList[i]; }
                ImGui.PopButtonRepeat();
                ImGui.SameLine();
                ImGui.Text($"{QuantityFoodList[i]}");
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.36f, 0.32f, 1));
                if (ImGui.Button($"Add##{i}", new Vector2(50f,25f)) && QuantityFoodList[i] >= 0)
                {
                    var already_existing_food = FoodList.Find(food => food.Name == foods[idx].Name);
                    GrandTotal += foods[idx].Price * QuantityFoodList[i];

                    if (already_existing_food != null)
                    {
                        already_existing_food.Amount += foods[idx].Amount * QuantityFoodList[i];
                    }
                    else
                    {
                        FoodList.Add(new Food(foods[idx].Price, foods[idx].Amount, foods[idx].Name, foods[idx].MenuItemId));
                        FoodList.Last().Amount *= QuantityFoodList[i];
                    }
                }
                ImGui.PopStyleColor();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Separator();
            }
        }


    }
}
