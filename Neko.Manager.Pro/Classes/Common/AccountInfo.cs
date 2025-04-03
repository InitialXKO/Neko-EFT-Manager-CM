/* AccountInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */

namespace Neko.EFT.Manager.X.Classes.Common;

public class AccountInfo
{
    public string id;
    public string nickname;
    public string username;
    public string password;
    public bool wipe;
    public string edition;

    public AccountInfo()
    {
        id = "";
        nickname = "";
        username = "";
        password = "";
        wipe = false;
        edition = "";
    }
}
