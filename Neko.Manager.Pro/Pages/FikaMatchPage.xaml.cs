using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.WindowManagement;
using CommunityToolkit.WinUI;
using Neko.EFT.Manager.X.Classes;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class FikaMatchPage : Page
    {
        private Dictionary<string, LineSeries> lineSeriesDictionary;
        public Frame contentFrame;
        public static string serverAddress = PlayPage.ServerAddress;
        public FikaMatchPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            lineSeriesDictionary = new Dictionary<string, LineSeries>();
            
            Loaded += async (sender, e) =>
            {
                await InitializeAsync();

            };
        }
        private async Task InitializeAsync()
        {
            //await ShowErrorDialog("错误", "该功能正在开发中.");
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            DisplayMatchData();



        }

        private async void DisplayMatchData()
        
        {
            try
            {
                var requestDataList = new List<string>
    {
        @"
        {
            ""keyId"":"""",
            ""location"":"""",
            ""timeVariant"":"""",
            ""side"":"""",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }"
    };

                //string url = "/coop/server/getAllForLocation";
                string url = "/fika/location/raids";
                string serverAddress = PlayPage.ServerAddress;
                var tasks = requestDataList.Select(requestData => NekoBackendTools.PostJsonAsync(url, requestData, serverAddress)).ToList();
                var responses = await Task.WhenAll(tasks);

                var groupedData = new Dictionary<string, List<JObject>>();

                foreach (var returnData in responses)
                {
                    if (!string.IsNullOrEmpty(returnData))
                    {
                        var matchData = JArray.Parse(returnData);
                        foreach (var item in matchData)
                        {
                            var location = item["location"]?.ToString();
                            var timeVariant = item["Time"]?.ToString();
                            var key = $"{location}_{timeVariant}";

                            if (!groupedData.ContainsKey(key))
                            {
                                groupedData[key] = new List<JObject>();
                            }
                            groupedData[key].Add((JObject)item);
                        }
                    }
                }

                if (groupedData.Count == 0)
                {
                    // 显示无战局信息
                    var textBlock = new TextBlock
                    {
                        Text = "无正在进行的战局。",
                        Margin = new Thickness(10)
                    };
                    this.FikaMatchPanel.Children.Add(textBlock);
                }
                else
                {
                    foreach (var group in groupedData)
                    {
                        var keyParts = group.Key.Split('_');
                        var location = keyParts[0];
                        var timeVariant = keyParts[1];

                        // 显示战局
                        var expander = new Expander
                        {
                            Header = $"{TranslateLocation(location)} - {TranslateTimeVariant(timeVariant)}",
                            Margin = new Thickness(0, 10, 0, 10),
                            Width = 500 // 设置固定宽度，这里假设为 500
                        };

                        var stackPanel = new StackPanel();

                        foreach (var item in group.Value)
                        {
                            var serverId = item["serverId"]?.ToString();
                            var hostName = item["hostUsername"]?.ToString();

                            var button = new Button
                            {
                                Content = $"主机:{hostName} — 战局ID:{serverId}",
                                Tag = item,
                                Margin = new Thickness(5)
                            };
                            button.Click += ShowMatchDetails;

                            stackPanel.Children.Add(button);
                        }

                        expander.Content = stackPanel;
                        FikaMatchPanel.Children.Add(expander);
                    }
                    FillMatchStatsListView(groupedData);
                }
            }
            catch (Exception ex)
            {
                await Utils.ShowInfoBar("错误", $"获取战局信息发生错误：{ex.Message}", InfoBarSeverity.Error);
                var textBlock = new TextBlock
                {
                    Text = $"获取战局信息发生错误：\n{ex.Message}",
                    Margin = new Thickness(10),
                    FontSize = 18
                };
                this.FikaMatchPanel.Children.Add(textBlock);
            }
           
            
        }



        private async Task ShowErrorDialog(string title, string content)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            contentDialog.Closed += ContentDialog_Closed;

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        }
        
        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {

            Frame.Navigate(typeof(PlayPage));
        }


        // 创建用于显示的数据对象
        public class MatchStatsItem
        {
            public string Location { get; set; }
            public string Time { get; set; }
            public int MatchCount { get; set; }
        }

        // 填充数据
        private void FillMatchStatsListView(Dictionary<string, List<JObject>> groupedData)
        {
            // 创建用于显示的数据集合
            var matchStatsItems = new List<MatchStatsItem>();

            // 遍历 groupedData，并将数据转换为 MatchStatsItem 添加到集合中
            foreach (var group in groupedData)
            {
                var keyParts = group.Key.Split('_');
                var location = TranslateLocation(keyParts[0]);
                var timeVariant = TranslateTimeVariant(keyParts[1]);
                var matchCount = group.Value.Count;

                matchStatsItems.Add(new MatchStatsItem
                {
                    Location = location,
                    Time = timeVariant,
                    MatchCount = matchCount
                });
            }

            // 将数据集合绑定到 ListView
            FikaMatchStatsListView.ItemsSource = matchStatsItems;
        }

        private async Task<string> DeleteMatch(string serverId, string serverAddress)
        {
            // 设置 SessionId
            SessionManager.SessionId = serverId;

            var jsonObj = new JObject();
            jsonObj.Add("serverId", serverId);
            jsonObj.Add("profileId", serverId);
            var returnData = await NekoBackendTools.PostJsonAsync("/fika/raid/leave", jsonObj.ToString(), serverAddress);

            // 判断响应是否成功
            return returnData;
        }




        private async void ShowMatchDetails(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var matchData = button.Tag as JObject;

            var content = GenerateDetailsPanel(matchData);

            ContentDialog contentDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "战局详情",
                Content = content,
                PrimaryButtonText = "结束战局",
                CloseButtonText = "取消"
            };

            contentDialog.PrimaryButtonClick += async (s, ev) =>
            {
                // 从 matchData 中获取 serverId
                serverAddress = PlayPage.ServerAddress;
                var serverId = matchData["serverId"]?.ToString();
                if (!string.IsNullOrEmpty(serverId))
                {
                    await Utils.ShowInfoBar("提示", $"结束的战局ID: {serverId}", InfoBarSeverity.Success);
                    await Utils.ShowInfoBar("提示", $"服务器: {serverAddress}", InfoBarSeverity.Success);
                    // 调用结束战局方法
                    var data = await DeleteMatch(serverId, serverAddress);

                    // 在此处处理结束战局的逻辑
                    await Utils.ShowInfoBar("提示", $"战局结束:  {data}", InfoBarSeverity.Success);
                }
            };

            contentDialog.CloseButtonClick += (s, ev) =>
            {
                // 在此处处理取消按钮点击事件
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        }

        private UIElement GenerateDetailsPanel(JObject matchData)
{
    var listView = new ListView();
    foreach (var prop in matchData.Properties())
    {
        var textBlock = new TextBlock
        {
            Text = $"{TranslatePropertyName(prop.Name)}: {prop.Value}",
            Margin = new Thickness(5)
        };
        listView.Items.Add(textBlock);
    }
    return listView;
}


        private async Task<bool> EndMatch(string serverId)
{
    // 构造要发送的 JSON 数据
    JObject jsonObj = new JObject();
    jsonObj.Add("serverId", serverId);
            jsonObj.Add("profileId", serverId);
            string requestData = jsonObj.ToString();

    // 发送 POST 请求并获取返回的数据
    string responseData = await NekoBackendTools.PostJsonAsync("/fika/raid/leave", requestData, PlayPage.ServerAddress);

    // 解析返回的数据，判断是否成功
    JObject responseJson = JObject.Parse(responseData);
    if (responseJson["response"]?.ToString() == "OK")
    {
        return true; // 表示结束成功
    }
    else
    {
        return false; // 表示结束失败
    }
}



        private static string TranslatePropertyName(string propertyName)
        {
            switch (propertyName)
            {
                case "serverId":
                    return "服务器 ID";
                case "hostUsername":
                    return "主机名";
                case "location":
                    return "地图";
                case "playerCount":
                    return "玩家数量";
                case "side":
                    return "玩家阵营";
                case "status":
                    return "玩家状态";
                default:
                    return propertyName;
            }
        }

        private static string TranslateLocation(string location)
        {
            switch (location)
            {
                case "factory4-day":
                    return "工厂 (白天)";
                case "factory4 night":
                    return "工厂 (夜晚)";
                case "Sandbox":
                    return "沙盒";
                case "Woods":
                    return "森林";
                case "Interchange":
                    return "互换";
                case "Shoreline":
                    return "海岸线";
                default:
                    return location;
            }
        }

        private static string TranslateTimeVariant(string timeVariant)
        {
            switch (timeVariant)
            {
                case "CURR":
                    return "白天";
                case "PAST":
                    return "夜晚";
                default:
                    return timeVariant;
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NavView_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
