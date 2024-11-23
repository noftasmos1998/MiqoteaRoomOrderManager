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
using Dalamud.Utility;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace MiqoteaRoomOrderManager.Windows;

public class MainWindow : Window, IDisposable
{
    private string LogoPath;
    private Plugin Plugin;
    public static List<Order> OrderList = new();
    static List<List<Food>> ListOfFoods = new();
    static List<string> ListOfFoodsName = new(new string[] { "Light Bites", "Platters", "Desserts", "Drinks" });
    public List<IPlayerCharacter> PlayerList = [];
    public int CurrentTabOpen = -1;
    public IPlayerCharacter? selectedPlayer = null;

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

    public class OrderTotalResponse
    {
        [JsonPropertyName("total")]
        public uint total { get; set; }
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
            this.PlayerList = this.Plugin.GetNearbyPlayers();
            ImGui.Begin("Player List");
            ImGui.BeginChild("", new Vector2(200, 400), true);
            for (var i = 0; i < this.PlayerList.Count; i++)
            {
                var player = this.PlayerList[i];

                var isSelected = selectedPlayer != null && selectedPlayer.Name.TextValue.Equals(player.Name.TextValue);

                if (ImGui.Selectable(player.Name.TextValue, isSelected))
                {
                    selectedPlayer = player;
                }
            }

            ImGui.EndChild();
            ImGui.BeginDisabled(selectedPlayer == null);
            if (ImGui.Button("Add"))
            {
                OrderList[CurrentTabOpen].Customers.Add(selectedPlayer);
                selectedPlayer = null;
            }
            ImGui.EndDisabled();
            ImGui.End();
            ImGui.Text("Total Shift Gil: ");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{Plugin.Configuration.totalShiftGil.ToString("N0")}");
            ImGui.SameLine(ImGui.GetWindowWidth() - 155);
            ImGui.Text("Player: ");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"{Plugin.Configuration.player.Name}");
            ImGui.Spacing();
            ImGui.Spacing();
            if (!Plugin.Configuration.shitStarted)
            {
                ImGui.Indent(253);
                ImGui.Spacing();
                ImGui.SetWindowFontScale(1.2f);
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Shift hasn't started yet!");
                ImGui.SetWindowFontScale(1f);
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Unindent(253);
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Indent(283);
                ImGui.SetWindowFontScale(1.2f);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.36f, 0.32f, 1));
                if (ImGui.Button($"Refresh", new Vector2(100f, 25f)))
                {
                    _ = Plugin.apiClient.GetAsync<ShiftResponse>(endpoint: "/api/v1/shifts/latest").ContinueWith(task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            var response = task.GetResultSafely();

                            if (response.IsActive)
                            {
                                Plugin.LoadMenu();
                                Plugin.Configuration.currentGil = Plugin.GetGilCount();
                                Plugin.Configuration.shitStarted = true;
                            }
                        }
                    });
                }
                ImGui.PopStyleColor();
                ImGui.SetWindowFontScale(1f);
                ImGui.Unindent(283);
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
                            CurrentTabOpen = n;
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

    public void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
            if (type is XivChatType.SystemMessage or (XivChatType)569)
        {
            if(message.TextValue.Contains("Trade request sent to ") || message.TextValue.Contains("Trade complete")) { 
                string messageText = message.TextValue;

                foreach (var order in OrderList)
                {
                    bool messageContainsCustomer = order.MidTrade || order.Customers.Any((customer) => messageText.Contains(customer.Name.TextValue));

                    if (!messageContainsCustomer)
                    {
                        continue;
                    }

                    if (messageText.Contains("Trade request sent to "))
                    {
                        this.Plugin.Configuration.currentGil = this.Plugin.GetGilCount();
                        order.MidTrade = true;
                    }

                    if (messageText.Contains("Trade complete"))
                    {
                        var newCurrentGil = this.Plugin.GetGilCount();
                        var gilDifference = newCurrentGil - this.Plugin.Configuration.currentGil;

                        if (gilDifference > 0)
                        {
                            order.TotalReceived += gilDifference;
                        }
                        order.MidTrade = false;
                    }

                    break;
                }
            }
        }
    }

    public class Order
    {
        private Plugin Plugin;
        public int GrandTotal = 0;
        public List<string> ClientList = [];
        public List<Food> FoodList = [];
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
        public int currentSelectedUser = 0;
        public List<IPlayerCharacter> Customers = [];
        public uint TotalReceived = 0;
        public bool MidTrade = false;

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
            var halfWidth = ImGui.GetWindowWidth() * 0.48f;
            ImGui.PushItemWidth(halfWidth);
            // Order list section
            ImGui.BeginChild("FoodListTableChild", new Vector2(halfWidth, 200), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            ImGui.Text("Order list");

            if (FoodList.Count > 0 && ImGui.BeginTable($"##{OrderName}", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInner))
            {
                for (var row = 0; row < FoodList.Count; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{FoodList[row].Name} x ");
                    ImGui.SameLine(0, 0);
                    ImGui.TextColored(new Vector4(0, 255, 0, 1), $"{FoodList[row].Amount}");

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0.05f));
                    ImGui.SameLine(ImGui.GetWindowWidth() - 42);
                    if (ImGui.Button($"X##{row}", new Vector2(30f, 20f)))
                    {
                        GrandTotal -= FoodList[row].Price * (FoodList[row].Amount / FoodList[row].BaseFoodAmount);
                        FoodList.RemoveAt(row);
                    }
                    ImGui.PopStyleColor();
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.SameLine();

            // Customer list section
            ImGui.BeginChild("SelectedUsersTable", new Vector2(halfWidth, 200), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            ImGui.Text("Customer list");
            if (Customers.Count > 0 && ImGui.BeginTable("##SelectedUsers", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInner))
            {
                for (var row = 0; row < Customers.Count; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(Customers[row].Name.TextValue);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0.05f));
                    ImGui.SameLine(ImGui.GetWindowWidth() - 42);
                    if (ImGui.Button($"X##{row}", new Vector2(30f, 20f)))
                    {
                        Customers.RemoveAt(row);
                    }
                    ImGui.PopStyleColor();
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();
            //ImGui.PopItemWidth();
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.16f, 0.65f, 0.02f, 1));
            if (ImGui.Button("Finish Order", new Vector2(100f, 25f)))
            {
                var requestBody = new OrderRequest(GrandTotal,TotalReceived, FoodList.Select(food => new OrderItem(food.MenuItemId, food.Amount)).ToArray());
                Plugin.apiClient.PostAsync<OrderRequest, OrderResponse>("/api/v1/orders", requestBody).ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        OrderList.RemoveAt(index);
                        Plugin.Configuration.totalReceived = 0;
                        Plugin.apiClient.GetAsync<OrderTotalResponse>("/api/v1/orders/total").ContinueWith(task => {
                            if (task.IsCompletedSuccessfully)
                            {
                                var response = task.GetResultSafely();
                                Plugin.Configuration.totalShiftGil = response.total;
                            }
                        });
                    }
                });
                
            }
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0.65f, 0, 1));
            if (ImGui.Button("Reset Order", new Vector2(100f, 25f)))
            {
                GrandTotal = 0;
                foodIdxLightBites = foodIdxDrinks = foodIdxDesserts = foodIdxPlatters = 0;
                QuantityFoodList = new(new int[] { 1, 1, 1, 1 });
                FoodList.Clear();
            }
            ImGui.PopStyleColor();  
            ImGui.EndTabItem();
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
                        FoodList.Add(new Food(foods[idx].Price, foods[idx].Amount, foods[idx].Name, foods[idx].MenuItemId, foods[idx].Amount));
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
