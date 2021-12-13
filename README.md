# C# ���ô�Į���ʵ�� QQ ������Ҳ���Ϸ����

ԭ�ĵ�ַ��<https://www.developerastrid.com/computer-vision/lets-find-the-difference/>

## һ��˼·

1. ���ô�Į��� `BindWindowEx` ��������Ϸ���ڣ�
2. ���ô�Į��� `Capture` ��ͼ�����һ�ţ��ұ�һ�ţ�
3. �Ա�����ͼƬ���ҳ���ͬ�ĵط���

## ������Į�������Ϸ����

�Ȼ�ȡ��Ϸ���ھ��

```csharp
//��ȡ���ھ��
var hwnd = dmSoft.FindWindow("#32770", "������Ҳ�");
if (hwnd == 0)
{
    throw new Exception("��ȡ���ھ��ʧ��");
}
```

Ȼ��󶨴���

```csharp
//�󶨴���
var bindWindowExResult = dmSoft.BindWindowEx(hwnd, "gdi", "normal", "normal", "", 0);

if (bindWindowExResult == 0)
{
    throw new Exception("�󶨴���ʧ��");
}
```

## ������Į�����ͼ

ʹ�ô�Į�ۺϹ����ҳ���Ҫ��ͼ�����꣬Ȼ�������

```csharp
//���(381,286)
var imgWidth = 381;
var imgHeight = 286;
var imgInterval = 76;//����ͼ�ļ��
var offsetTop = 312;//��ƫ��
var offsetLeft = 93;//��ƫ��

var x1 = offsetLeft;
var y1 = offsetTop;
var x2 = x1 + imgWidth;
var y2 = y1 + imgHeight;

var x3 = x2 + imgInterval;
var y3 = y1;
var x4 = x3 + imgWidth;
var y4 = y2;
```

ʹ�� `Capture` ������ͼ

```csharp
//��ͼ���
var leftFileName = "leftFile.bmp";
dmSoft.Capture(x1, y1, x2, y2, leftFileName);

//��ͼ�ұ�
var rightFileName = "rightFile.bmp";
dmSoft.Capture(x3, y3, x4, y4, rightFileName);
```

## �ġ��ҳ�����ͼƬ����ͬ�ĵط�

��Ҫ��ȡͼƬÿ�����ص���ɫ���ݣ�Ȼ���ٶԱȡ�

��װ `GetRgbValueBytes` ���������ڻ�ȡͼƬ����ɫ����

```csharp
private static byte[] GetRgbValueBytes(Bitmap bitmap)
{
    // �� Bitmap ������ϵͳ�ڴ���
    Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
    bitmap.UnlockBits(bitmapData);

    // ��ȡ��һ�еĵ�ַ
    IntPtr ptr = bitmapData.Scan0;

    // ����һ������������λͼ���ֽ�
    int length = Math.Abs(bitmapData.Stride) * bitmapData.Height;
    byte[] rgbValues = new byte[length];

    // ���� RGB ֵ��������
    Marshal.Copy(ptr, rgbValues, 0, length);
    return rgbValues;
}
```

��װ `GetDifferent` �����������ҳ�����ͼƬ��ͬ�Ĳ��֣����Ѳ�ͬ�Ĳ�����Ϊ `Bitmap` ���ͷ���

```csharp
/// <summary>
/// ��ȡ����ͼƬ�Ĳ��죬�����첿�ֱ�ǵ�һ���µ�ͼƬ������������ͼƬ
/// </summary>
/// <param name="bitmap1">��һ��ͼƬ</param>
/// <param name="bitmap2">�ڶ���ͼƬ</param>
/// <returns>����ͼƬ</returns>
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

    // �ݲ�
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


        // �ѷ�����������ɫ��Ϊ��ɫ�������Ϊ��ɫ
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

    // ����һ���µ�ͼƬ
    Bitmap bitmap3 = new Bitmap(bitmap1.Width, bitmap1.Height, bitmap2.PixelFormat);

    BitmapData bitmapData = GetBitmapData(bitmap3);

    Marshal.Copy(rgbValues, 0, bitmapData.Scan0, rgbValues.Length);

    bitmap1.Dispose();
    bitmap2.Dispose();

    return bitmap3;
}
```

## �塢��ʾ��ͬ���ֵ�ͼƬ

���ô�Į��� `CreateFoobarCustom`��������ͼƬ��ʾ����

```c
//���
foobar = dmSoft.CreateFoobarCustom(hwnd, x1, y1, tempFileName, "000000", 1.0);
dmSoft.FoobarFillRect(foobar, 0, 0, x2, y2, "ff0000");
dmSoft.FoobarUpdate(foobar);
```

## ������ʾ

��ͼ�У���ɫ�����������ͼƬ��֮ͬ����

![C# ���ô�Į���ʵ�� QQ ������Ҳ���Ϸ����](https://cdn.developerastrid.com/202112131615353.png)