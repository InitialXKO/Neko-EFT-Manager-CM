using System;
using System.IO;

namespace Neko.EFT.Manager.X.Classes
{
    internal class LegalGameCheckWrapper
    {
        public static bool Checked { get; private set; } = false;

        public static byte[] LegalGameFound { get; private set; } = new byte[4];

        public static void SimulateLegalCheck()
        {
            try
            {
                // 模拟合法性检查为真
                Checked = true;

                // 在这里可以添加其他模拟合法性检查的逻辑

                return;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"Error during simulated legality check: {ex.Message}");
            }

            Checked = true;
            Console.WriteLine("Simulated legality check failed.");
        }

        // 以下是 LegalGameCheck 中其他相关方法的模拟，你可以根据需要自行添加

        private static void ProcessLGF(bool v)
        {
            LegalGameFound[0] = Convert.ToByte(v);
            LegalGameFound[1] = Convert.ToByte(!v);
            LegalGameFound[2] = 0x1 << 0x1;
            LegalGameFound[3] = 0x0 << 0x1;
        }

        internal static bool LC1A(string gfp)
        {
            var fiGFP = new FileInfo(gfp);
            return (fiGFP.Exists && fiGFP.Length >= 647 * 1000);
        }

        internal static bool LC2B(string gfp)
        {
            var fiBE = new FileInfo(gfp.Replace(".exe", "_BE.exe"));
            return (fiBE.Exists && fiBE.Length >= 1024000);
        }

        internal static bool LC3C(string gfp)
        {
            var diBattlEye = new DirectoryInfo(gfp.Replace("EscapeFromTarkov.exe", "BattlEye"));
            return (diBattlEye.Exists);
        }
    }
}
