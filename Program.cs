using FindDifferenceConsoleApp.Configs;
using FindDifferenceConsoleApp.DmSoft;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FindDifferenceConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.Is64BitProcess)
            {
                Console.WriteLine("这是 64 位程序");
                Console.WriteLine("按任意键结束程序");
                Console.ReadKey();
                return;
            }

            // 免注册调用大漠插件
            var registerDmSoftDllResult = RegisterDmSoft.RegisterDmSoftDll();
            //Console.WriteLine($"免注册调用大漠插件返回：{registerDmSoftDllResult}");
            if (!registerDmSoftDllResult)
            {
                throw new Exception("免注册调用大漠插件失败");
            }

            // 创建对象
            DmSoftCustomClassName dmSoft = new DmSoftCustomClassName();

            // 收费注册
            var regResult = dmSoft.Reg(DmConfig.DmRegCode, DmConfig.DmVerInfo);
            //Console.WriteLine($"收费注册返回：{regResult}");
            if (regResult != 1)
            {
                throw new Exception("收费注册失败");
            }

            // 判断 Resources 是否存在，不存在就创建
            if (!Directory.Exists(DmConfig.DmGlobalPath))
            {
                Directory.CreateDirectory(DmConfig.DmGlobalPath);
            }

            // 设置全局路径,设置了此路径后,所有接口调用中,相关的文件都相对于此路径. 比如图片,字库等
            dmSoft.SetPath(DmConfig.DmGlobalPath);

            Console.Write("请在找茬界面按任意键继续");
            Console.ReadKey();


            var foobar = 0;
            do
            {
                Console.WriteLine();
                dmSoft.UnBindWindow();
                dmSoft.FoobarClose(foobar);
                dmSoft.delay(200);

                //获取窗口句柄
                var hwnd = dmSoft.FindWindow("#32770", "大家来找茬");
                if (hwnd == 0)
                {
                    throw new Exception("获取窗口句柄失败");
                }

                //绑定窗口
                var bindWindowExResult = dmSoft.BindWindowEx(hwnd, "gdi", "normal", "normal", "", 0);

                if (bindWindowExResult == 0)
                {
                    throw new Exception("绑定窗口失败");
                }

                //截图
                //宽高(381,286)
                var imgWidth = 381;
                var imgHeight = 286;
                var imgInterval = 76;//两张图的间隔
                var offsetTop = 312;//左偏移
                var offsetLeft = 93;//左偏移

                var x1 = offsetLeft;
                var y1 = offsetTop;
                var x2 = x1 + imgWidth;
                var y2 = y1 + imgHeight;

                var x3 = x2 + imgInterval;
                var y3 = y1;
                var x4 = x3 + imgWidth;
                var y4 = y2;

                //截图左边
                var leftFileName = "leftFile.bmp";
                dmSoft.Capture(x1, y1, x2, y2, leftFileName);

                //截图右边
                var rightFileName = "rightFile.bmp";
                dmSoft.Capture(x3, y3, x4, y4, rightFileName);

                //处理图片
                Bitmap bitmap = GetDifferent(
                    new Bitmap(Path.Combine(DmConfig.DmGlobalPath, leftFileName)),
                    new Bitmap(Path.Combine(DmConfig.DmGlobalPath, rightFileName))
                    );

                var tempFileName = $"{Guid.NewGuid():N}.bmp";
                bitmap.Save(Path.Combine(DmConfig.DmGlobalPath, tempFileName), ImageFormat.Bmp);
                bitmap.Dispose();

                //标记
                foobar = dmSoft.CreateFoobarCustom(hwnd, x1, y1, tempFileName, "000000", 1.0);
                dmSoft.FoobarFillRect(foobar, 0, 0, x2, y2, "ff0000");
                dmSoft.FoobarUpdate(foobar);

                // 删除临时图片
                File.Delete(Path.Combine(DmConfig.DmGlobalPath, tempFileName));


                Console.Write("是否继续？（y/n）");
            } while (Console.ReadKey().Key == ConsoleKey.Y);

            Console.WriteLine("按任意键结束程序");
            Console.ReadKey();
        }



        private static byte[] GetRgbValueBytes(Bitmap bitmap)
        {
            BitmapData bitmapData = GetBitmapData(bitmap);
            IntPtr ptr = bitmapData.Scan0;

            int length = Math.Abs(bitmapData.Stride) * bitmapData.Height;
            byte[] rgbValues = new byte[length];

            Marshal.Copy(ptr, rgbValues, 0, length);
            return rgbValues;
        }

        private static BitmapData GetBitmapData(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            bitmap.UnlockBits(bitmapData);
            return bitmapData;
        }

        /// <summary>
        /// 获取两张图片的差异，将差异部分标记到一张新的图片，并返回这张图片
        /// </summary>
        /// <param name="bitmap1">第一张图片</param>
        /// <param name="bitmap2">第二张图片</param>
        /// <returns>差异图片</returns>
        private static Bitmap GetDifferent(Bitmap bitmap1, Bitmap bitmap2)
        {
            var bitmap1RgbValue = GetRgbValueBytes(bitmap1);
            var bitmap2RgbValue = GetRgbValueBytes(bitmap2);

            byte r1;
            byte g1;
            byte b1;

            byte r2;
            byte g2;
            byte b2;

            byte r3;
            byte g3;
            byte b3;

            var allowance = 40;

            byte[] rgbValues = new byte[bitmap1RgbValue.Length];
            for (int i = 0; i < bitmap1RgbValue.Length - 1; i += 3)
            {
                r1 = r3 = bitmap1RgbValue[i + 2];
                g1 = g3 = bitmap1RgbValue[i + 1];
                b1 = b3 = bitmap1RgbValue[i];

                r2 = bitmap2RgbValue[i + 2];
                g2 = bitmap2RgbValue[i + 1];
                b2 = bitmap2RgbValue[i];


                if ((r1 + allowance <= r2 || r1 - allowance >= r2)
                    || (g1 + allowance <= g2 || g1 - allowance >= g2)
                    || (b1 + allowance <= b2 || b1 - allowance >= b2))
                {
                    r3 = 255;
                    g3 = 255;
                    b3 = 255;
                }
                else
                {
                    r3 = 0;
                    g3 = 0;
                    b3 = 0;
                }

                rgbValues[i + 2] = r3;
                rgbValues[i + 1] = g3;
                rgbValues[i] = b3;
            }

            Bitmap bitmap3 = new Bitmap(bitmap1.Width, bitmap1.Height, bitmap2.PixelFormat);

            BitmapData bitmapData = GetBitmapData(bitmap3);

            Marshal.Copy(rgbValues, 0, bitmapData.Scan0, rgbValues.Length);

            bitmap1.Dispose();
            bitmap2.Dispose();

            return bitmap3;
        }


    }
}
