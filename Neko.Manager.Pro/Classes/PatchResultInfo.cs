/* PatchResultInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: Merijn Hendriks
 * AUTHORS:
 * waffle.lord
 */

using Neko.EFT.Manager.X.Classes;

namespace Neko.EFT.Manager.X.Classes
{
    public class PatchResultInfo
    {
        public PatchResultType Status { get; }
        
        public int NumCompleted { get; }
        
        public int NumTotal { get; }

        public bool OK => (Status == PatchResultType.Success) || (Status == PatchResultType.AlreadyPatched);
        
        public int PercentComplete => (NumCompleted * 100) / NumTotal;

        public PatchResultInfo(PatchResultType Status, int NumCompleted, int NumTotal)
        {
            this.Status = Status;
            this.NumCompleted = NumCompleted;
            this.NumTotal = NumTotal;
        }
    }
}
