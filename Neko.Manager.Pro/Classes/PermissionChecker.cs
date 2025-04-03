using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;

namespace Neko.EFT.Manager.X.Classes
{
    public class PermissionChecker
    {
        // 判断当前程序是否具有管理员权限
        public static string GetPermissionMode()
        {
            // 获取当前 Windows 身份
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            // 判断是否为管理员角色
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            // 返回相应的权限模式
            return isAdmin ? "系统管理员模式" : "用户模式";
        }
    }
}
