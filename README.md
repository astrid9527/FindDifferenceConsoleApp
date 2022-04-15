# C# 调用大漠插件实现 QQ 大家来找茬游戏辅助

原文地址：<https://www.developerastrid.com/computer-vision/csharp-dm-find-fault/>

## 一、思路

1. 调用大漠插件 `BindWindowEx` 方法绑定游戏窗口；
2. 调用大漠插件 `Capture` 截图，左边一张，右边一张；
3. 对比两张图片，找出不同的地方。

## 二、大漠插件绑定游戏窗口

先获取游戏窗口句柄

```csharp
//获取窗口句柄
var hwnd = dmSoft.FindWindow("#32770", "大家来找茬");
if (hwnd == 0)
{
    throw new Exception("获取窗口句柄失败");
}
```

然后绑定窗口

```csharp
//绑定窗口
var bindWindowExResult = dmSoft.BindWindowEx(hwnd, "gdi", "normal", "normal", "", 0);

if (bindWindowExResult == 0)
{
    throw new Exception("绑定窗口失败");
}
```

## 三、大漠插件截图

使用大漠综合工具找出需要截图的坐标，然后记下来

```csharp
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
```

使用 `Capture` 方法截图

```csharp
//截图左边
var leftFileName = "leftFile.bmp";
dmSoft.Capture(x1, y1, x2, y2, leftFileName);

//截图右边
var rightFileName = "rightFile.bmp";
dmSoft.Capture(x3, y3, x4, y4, rightFileName);
```

## 四、找出两张图片不相同的地方

先要获取图片每个像素的颜色数据，然后再对比。

封装 `GetRgbValueBytes` 方法，用于获取图片的颜色数据

```csharp
private static byte[] GetRgbValueBytes(Bitmap bitmap)
{
    // 将 Bitmap 锁定到系统内存中
    Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
    bitmap.UnlockBits(bitmapData);

    // 获取第一行的地址
    IntPtr ptr = bitmapData.Scan0;

    // 声明一个数组来保存位图的字节
    int length = Math.Abs(bitmapData.Stride) * bitmapData.Height;
    byte[] rgbValues = new byte[length];

    // 复制 RGB 值到数组中
    Marshal.Copy(ptr, rgbValues, 0, length);
    return rgbValues;
}
```

封装 `GetDifferent` 方法，用于找出两张图片不同的部分，并把不同的部分作为 `Bitmap` 类型返回

```csharp
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

    // 容差
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


        // 把符合条件的颜色改为白色，否则改为黑色
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

    // 生成一张新的图片
    Bitmap bitmap3 = new Bitmap(bitmap1.Width, bitmap1.Height, bitmap2.PixelFormat);

    BitmapData bitmapData = GetBitmapData(bitmap3);

    Marshal.Copy(rgbValues, 0, bitmapData.Scan0, rgbValues.Length);

    bitmap1.Dispose();
    bitmap2.Dispose();

    return bitmap3;
}
```

## 五、显示不同部分的图片

调用大漠插件 `CreateFoobarCustom`，将差异图片显示出来

```c
//标记
foobar = dmSoft.CreateFoobarCustom(hwnd, x1, y1, tempFileName, "000000", 1.0);
dmSoft.FoobarFillRect(foobar, 0, 0, x2, y2, "ff0000");
dmSoft.FoobarUpdate(foobar);
```

## 六、演示

下图中，红色区域就是两张图片不同之处。

![C# 调用大漠插件实现 QQ 大家来找茬游戏辅助](https://cdn.developerastrid.com/202112131615353.png)
