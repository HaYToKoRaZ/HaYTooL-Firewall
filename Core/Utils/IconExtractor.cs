using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GuvenlikDuvarim.Core.Utils
{
    public static class IconExtractor
    {
        private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource GetIcon(string path, bool isFolder)
        {
            string key = (isFolder ? "DIR:" : "EXE:") + (path ?? string.Empty);
            if (IconCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            ImageSource? extracted = ExtractIconInternal(path ?? string.Empty, isFolder);
            if (extracted != null)
            {
                IconCache[key] = extracted;
                return extracted;
            }

            var fallback = CreateFallbackBitmap(isFolder);
            IconCache[key] = fallback;
            return fallback;
        }

        private static ImageSource? ExtractIconInternal(string path, bool isFolder)
        {
            try
            {
                // Method 1: For existing EXE files, extract associated application icon
                if (!isFolder && File.Exists(path))
                {
                    using Icon? sysIcon = Icon.ExtractAssociatedIcon(path);
                    if (sysIcon != null)
                    {
                        return ConvertHIconToBitmapSource(sysIcon.Handle);
                    }
                }

                // Method 2: Shell API for Folders or virtual paths
                SHFILEINFO shfi = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;
                uint attrs = 0;

                if (isFolder)
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                    attrs = FILE_ATTRIBUTE_DIRECTORY;
                    path = "folder";
                }
                else if (!File.Exists(path))
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                    attrs = FILE_ATTRIBUTE_NORMAL;
                    path = "file.exe";
                }

                IntPtr hResult = SHGetFileInfo(path, attrs, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
                if (hResult != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        return ConvertHIconToBitmapSource(shfi.hIcon);
                    }
                    finally
                    {
                        DestroyIcon(shfi.hIcon);
                    }
                }
            }
            catch { }

            return null;
        }

        private static BitmapSource ConvertHIconToBitmapSource(IntPtr hIcon)
        {
            BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bs.Freeze();
            return bs;
        }

        private static ImageSource CreateFallbackBitmap(bool isFolder)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                if (isFolder)
                {
                    dc.DrawRoundedRectangle(System.Windows.Media.Brushes.Goldenrod, null, new Rect(1, 4, 14, 10), 1, 1);
                    dc.DrawRoundedRectangle(System.Windows.Media.Brushes.DarkGoldenrod, null, new Rect(1, 2, 6, 4), 1, 1);
                }
                else
                {
                    dc.DrawRoundedRectangle(System.Windows.Media.Brushes.DodgerBlue, null, new Rect(2, 1, 12, 14), 1, 1);
                }
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }
    }
}
