// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tools.Helpers;

public static class IconHelper {
    /// <summary>
    /// 将ImageSource转换为Base64字符串
    /// </summary>
    public static string ImageSourceToBase64(ImageSource imageSource) {
        try {
            var bitmapSource = imageSource as BitmapSource;
            if (bitmapSource == null) return string.Empty;

            // 选择编码器（PNG支持透明，JPEG更小）
            BitmapEncoder encoder = new PngBitmapEncoder();
            // 或者使用JPEG（文件更小，但不支持透明）
            // BitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        } catch (Exception ex) {
            // 记录错误但不抛出异常
            System.Diagnostics.Debug.WriteLine($"转换ImageSource到Base64失败: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 将Base64字符串转换回ImageSource
    /// </summary>
    public static ImageSource? Base64ToImageSource(string base64String) {
        if (string.IsNullOrEmpty(base64String)) {
            return null;
        }

        try {
            byte[] imageBytes = Convert.FromBase64String(base64String);

            using var memoryStream = new MemoryStream(imageBytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // 使其可跨线程访问

            return bitmapImage;
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"转换Base64到ImageSource失败: {ex.Message}");
            return CreateDefaultIcon();
        }
    }

    /// <summary>
    /// 创建默认图标（用于错误情况）
    /// </summary>
    private static ImageSource CreateDefaultIcon() {
        try {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen()) {
                drawingContext.DrawRectangle(
                    Brushes.Gray,
                    new Pen(Brushes.DarkGray, 1),
                    new Rect(0, 0, 32, 32));

                // 使用推荐的构造函数
                var formattedText = new FormattedText(
                        "?",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        12,
                        Brushes.White,
                        GetPixelsPerDip()) // 添加 PixelsPerDip 参数
                    {
                        TextAlignment = TextAlignment.Center
                    };

                // 计算文本居中位置
                double x = (32 - formattedText.Width) / 2;
                double y = (32 - formattedText.Height) / 2;

                drawingContext.DrawText(formattedText, new Point(x, y));
            }

            var renderTarget = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();

            return renderTarget;
        } catch (Exception ex) {
            throw new Exception("创建默认图标失败", ex);
        }
    }

    /// <summary>
    /// 获取当前系统的 PixelsPerDip 值
    /// </summary>
    private static double GetPixelsPerDip() {
        try {
            return Application.Current!.MainWindow != null
                ? VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip
                : 1.0;
        } catch {
            return 1.0;
        }
    }

    /// <summary>
    /// 压缩Base64图标数据（如果太大）
    /// </summary>
    public static string CompressIconData(string base64IconData, int maxSizeKb = 10) {
        if (string.IsNullOrEmpty(base64IconData) || base64IconData.Length <= maxSizeKb * 1024 * 4 / 3)
            return base64IconData;

        try {
            // 如果图标数据太大，重新编码为更小的格式
            var imageSource = Base64ToImageSource(base64IconData);
            if (imageSource == null) return base64IconData;

            // 转换为JPEG格式（更小）
            var bitmapSource = imageSource as BitmapSource;
            if (bitmapSource == null) return base64IconData;

            var encoder = new JpegBitmapEncoder { QualityLevel = 75 }; // 质量75%
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            byte[] compressedBytes = memoryStream.ToArray();
            return Convert.ToBase64String(compressedBytes);
        } catch {
            return base64IconData; // 压缩失败，返回原数据
        }
    }
}