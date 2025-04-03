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

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class MatchPage : Page
    {
        private Dictionary<string, LineSeries> lineSeriesDictionary;
        public Frame contentFrame;
        public static string serverAddress = PlayPage.ServerAddress;
        public MatchPage()
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
            var requestDataList = new List<string>
    {
        @"
        {
            ""keyId"":"""",
            ""location"":""factory4_day"",
            ""timeVariant"":""CURR"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""factory4_night"",
            ""timeVariant"":""PAST"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Sandbox"",
            ""timeVariant"":""PAST"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Sandbox"",
            ""timeVariant"":""CURR"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Woods"",
            ""timeVariant"":""CURR"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Woods"",
            ""timeVariant"":""PAST"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Interchange"",
            ""timeVariant"":""CURR"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Interchange"",
            ""timeVariant"":""PAST"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Shoreline"",
            ""timeVariant"":""CURR"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }",
        @"
        {
            ""keyId"":"""",
            ""location"":""Shoreline"",
            ""timeVariant"":""PAST"",
            ""side"":""Pmc"",
            ""raidMode"":""Local"",
            ""MaxGroupCount"":6
        }"
    };

            //string url = "/coop/server/getAllForLocation";
            string url = "/coop/server/getAllForLocation";
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
                        var location = item["Location"]?.ToString();
                        var timeVariant = item["TimeVariant"]?.ToString();
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
                MatchPanel.Children.Add(textBlock);
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
                        var serverId = item["ServerId"]?.ToString();
                        var hostName = item["HostName"]?.ToString();

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
                    MatchPanel.Children.Add(expander);
                }
                FillMatchStatsListView(groupedData);
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
            public string TimeVariant { get; set; }
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
                    TimeVariant = timeVariant,
                    MatchCount = matchCount
                });
            }

            // 将数据集合绑定到 ListView
            MatchStatsListView.ItemsSource = matchStatsItems;
        }

        private async Task<string> DeleteMatch(string serverId, string serverAddress)
        {
            // 设置 SessionId
            SessionManager.SessionId = serverId;

            var jsonObj = new JObject();
            jsonObj.Add("serverId", serverId);
            var returnData = await NekoBackendTools.PostJsonAsync("/coop/server/delete", jsonObj.ToString(), serverAddress);

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
                var serverId = matchData["ServerId"]?.ToString();
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
    string requestData = jsonObj.ToString();

    // 发送 POST 请求并获取返回的数据
    string responseData = await NekoBackendTools.PostJsonAsync("/coop/server/delete", requestData, PlayPage.ServerAddress);

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
                case "ServerId":
                    return "服务器 ID";
                case "HostProfileId":
                    return "主机用户 ID";
                case "HostName":
                    return "主机名";
                case "Location":
                    return "地图";
                case "PlayerCount":
                    return "玩家数量";
                case "ExpectedPlayerCount":
                    return "预期玩家数量";
                case "GameVersion":
                    return "游戏版本";
                case "SITVersion":
                    return "SIT版本";
                case "IsPasswordLocked":
                    return "密码状态";
                case "Protocol":
                    return "连接协议";
                case "Status":
                    return "状态";
                case "Settings":
                    return "设置";
                default:
                    return propertyName;
            }
        }

        private static string TranslateLocation(string location)
        {
            switch (location)
            {
                case "factory4_day":
                    return "工厂 (白天)";
                case "factory4_night":
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
                    return "当前";
                case "PAST":
                    return "过去";
                default:
                    return timeVariant;
            }
        }
    }
}
