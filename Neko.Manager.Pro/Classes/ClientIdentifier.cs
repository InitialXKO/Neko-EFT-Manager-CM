using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.System.Profile;

namespace Neko.EFT.Manager.X.Classes;
public static class ClientIdentifier
{
    /// <summary>
    /// 获取设备的硬件标识，并转换为十六进制字符串。如果获取不到，则返回一个随机生成的 Guid 字符串。
    /// </summary>
    public static string GetUniqueClientId()
    {
        var systemId = SystemIdentification.GetSystemIdForPublisher();
        if (systemId == null || systemId.Id.Length == 0)
        {
            // 如果无法获取系统硬件 ID，则退化为生成一个 Guid
            return Guid.NewGuid().ToString();
        }
        // 将二进制数组转换为十六进制字符串
        return CryptographicBuffer.EncodeToHexString(systemId.Id);
    }
}
