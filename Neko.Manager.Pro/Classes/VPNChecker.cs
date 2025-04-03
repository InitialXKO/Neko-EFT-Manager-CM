using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

public class VPNChecker
{
    public bool IsProxyEnabled()
    {
        try
        {
            const string registryKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, false))
            {
                if (registryKey != null)
                {
                    object proxyEnableValue = registryKey.GetValue("ProxyEnable");
                    if (proxyEnableValue != null && (int)proxyEnableValue == 1)
                    {
                        object proxyServerValue = registryKey.GetValue("ProxyServer");
                        if (proxyServerValue != null)
                        {
                            string proxyServer = proxyServerValue.ToString();
                            Debug.WriteLine($"检测到代理: {proxyServer}");
                            return true; // 系统代理已启用
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking proxy: {ex.Message}");
        }
        Debug.WriteLine($"未检测到代理");
        return false; // 系统代理未启用
    }

    public bool IsVPNConnected()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                // 更精确的 VPN 检测逻辑，只考虑包含 "VPN" 的名称或描述
                if ((ni.Description.ToLower().Contains("vpn") || ni.Name.ToLower().Contains("vpn")) &&
                    !(ni.Description.ToLower().Contains("virtual") ||
                      ni.Description.ToLower().Contains("vm") ||
                      ni.Name.ToLower().Contains("virtual") ||
                      ni.Name.ToLower().Contains("vm") ||
                      ni.Description.ToLower().Contains("radmin") ||
                      ni.Name.ToLower().Contains("radmin")))
                {
                    Debug.WriteLine($"Detected VPN connection: {ni.Name} - {ni.Description}");
                    return true; // VPN 已连接
                }
            }
        }
        Debug.WriteLine($"未检测到VPN");
        return false; // VPN 未连接
    }

    public bool IsUsingProxyOrVPN()
    {
        return IsProxyEnabled() || IsVPNConnected();
    }
}
