// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using Tools.Services.IServices;

namespace Tools.Services;

public class BmpSteganographyService : IBmpSteganographyService {
    public async Task Hide(string bmpPath, string fileToHide, string outputFolderPath,
        Action<double>? updateProgressPercent = null, Action<string>? updateProgressText = null,
        Action<string>? notifyOutputBmpPath = null) {
        try {
            updateProgressPercent?.Invoke(0.0);
            updateProgressText?.Invoke("正在读取Bmp文件...");

            // 读取BMP文件
            var bmpFile = await ReadBmpAsync(bmpPath, updateProgressPercent, 0.0, 0.2);

            updateProgressText?.Invoke("读取Bmp文件完成，正在准备读取隐藏文件...");

            // 读取要隐藏的文件
            var hideFile = await ReadFileToHideAsync(fileToHide, updateProgressPercent, 0.2, 0.1);

            updateProgressText?.Invoke("读取隐藏文件完成，正在准备数据头...");

            // 创建数据头
            var extension = GetFileExtension(fileToHide);
            var dataHeader = CreateDataHeader(extension, (uint)hideFile.Size);

            updateProgressText?.Invoke("创建数据头完成，正在准备嵌入数据...");

            // 准备隐藏数据
            var headerBits = StructToBits(dataHeader);
            var fileBits = BytesToBits(hideFile.Data);

            updateProgressText?.Invoke("准备嵌入数据完成，正在验证数据容量...");

            // 计算容量并验证
            var availableBits = CalculateAvailableSpace(bmpFile);
            if (headerBits.Count + fileBits.Count > availableBits) {
                throw new Exception("文件大小超出隐写上限，上限为：" + availableBits / 8 + "字节");
            }

            updateProgressText?.Invoke("验证数据容量完成，正在嵌入数据...");

            // 嵌入数据
            await EmbedDataAsync(bmpFile.ImageData, headerBits, fileBits, updateProgressPercent, 0.3, 0.5);

            updateProgressText?.Invoke("嵌入数据完成，正在写入Bmp文件...");

            // 自动生成输出文件名
            var originalBmpName = Path.GetFileNameWithoutExtension(bmpPath);
            var outputBmpPath = Path.Combine(outputFolderPath, originalBmpName + "_hided.bmp");

            // 如果文件已存在，添加数字后缀
            outputBmpPath = GetUniqueFileName(outputBmpPath);

            // 写入新的BMP文件
            await WriteBmpAsync(outputBmpPath, bmpFile, updateProgressPercent, 0.8, 0.2);
            updateProgressPercent?.Invoke(1.0);
            updateProgressText?.Invoke("写入Bmp文件完成，已生成文件");
            notifyOutputBmpPath?.Invoke(outputBmpPath);
        } catch (Exception e) {
            throw new Exception("隐藏文件失败: " + e.Message);
        }
    }

    public async Task Extract(string bmpPath, string outputFolderPath, Action<double>? updateProgressPercent = null,
        Action<string>? updateProgressText = null) {
        try {
            updateProgressPercent?.Invoke(0.0);
            updateProgressText?.Invoke("正在读取Bmp文件...");

            // 读取BMP文件
            var bmpFile = await ReadBmpAsync(bmpPath, updateProgressPercent, 0.0, 0.3);

            updateProgressText?.Invoke("读取Bmp文件完成，正在提取数据头...");
            // 提取数据头
            var (dataHeader, headerEnd) = ExtractDataHeader(bmpFile.ImageData);

            updateProgressPercent?.Invoke(0.3);
            updateProgressText?.Invoke("提取数据头完成，正在验证数据头...");

            // 验证数据头
            if (!ValidateDataHeader(dataHeader)) {
                throw new Exception("校验码错误");
            }

            updateProgressPercent?.Invoke(0.4);
            updateProgressText?.Invoke("验证数据头完成，正在提取文件数据...");

            // 提取文件数据
            var fileBits = await ExtractFileBitsAsync(bmpFile.ImageData, headerEnd, dataHeader.Size,
                updateProgressPercent, 0.4, 0.4);

            updateProgressPercent?.Invoke(0.8);
            updateProgressText?.Invoke("提取文件数据完成，正在写入文件...");

            var fileData = BitsToBytes(fileBits);

            // 自动生成输出文件名
            var originalFileName = Path.GetFileNameWithoutExtension(bmpPath);
            var outputFilePath = Path.Combine(outputFolderPath, originalFileName + "_extracted");

            // 写入输出文件
            await WriteExtractedFileAsync(outputFilePath, dataHeader.Extension, fileData,
                updateProgressPercent, 0.8, 0.2);

            updateProgressPercent?.Invoke(1.0);
            updateProgressText?.Invoke("写入文件完成，已生成文件");
        } catch (Exception e) {
            throw new Exception("提取文件失败: " + e.Message);
        }
    }

    public async Task<bool> Verify(string bmpPath, Action<double>? updateProgressPercent = null,
        Action<string>? updateProgressText = null) {
        try {
            updateProgressPercent?.Invoke(0.0);
            updateProgressText?.Invoke("正在读取Bmp文件...");

            // 读取BMP文件
            var bmpFile = await ReadBmpAsync(bmpPath, updateProgressPercent, 0.0, 0.7);

            updateProgressPercent?.Invoke(0.7);
            updateProgressText?.Invoke("读取Bmp文件完成，正在提取数据头...");

            // 提取数据头
            var result = ExtractDataHeader(bmpFile.ImageData);
            var dataHeader = result.Item1;

            updateProgressPercent?.Invoke(0.9);
            updateProgressText?.Invoke("提取数据头完成，正在验证数据头...");

            // 验证数据头
            var isValid = ValidateDataHeader(dataHeader);

            updateProgressPercent?.Invoke(1.0);
            updateProgressText?.Invoke("验证数据头完成，数据头验证结果：" + (isValid ? "校验正确" : "校验失败"));

            return isValid;
        } catch (Exception e) {
            throw new Exception("验证文件失败" + e.Message);
        }
    }

    public async Task<long> GetMaxHideSize(string bmpPath) {
        try {
            // 读取BMP文件
            var bmpFile = await ReadBmpAsync(bmpPath);

            // 计算可用空间（减去头部占用的160位）
            var availableBits = CalculateAvailableSpace(bmpFile);

            // 返回可用字节数（减去头部占用的20字节）
            return availableBits / 8 - 20;
        } catch (Exception e) {
            throw new Exception("计算最大隐写大小失败: " + e.Message);
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BmpFileHeader {
        public ushort FileType;
        public uint FileSize;
        public ushort Reserved1;
        public ushort Reserved2;
        public uint OffsetData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BmpInfoHeader {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPixelsPerMeter;
        public int YPixelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    private struct HideDataHeader {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Extension;
    }

    private class BmpFile {
        public BmpFileHeader FileHeader;
        public BmpInfoHeader InfoHeader;
        public List<byte> ImageData = [];
    }

    private class HideFile {
        public long Size;
        public List<byte> Data = [];
    }

    // 异步读取BMP文件结构
    private async Task<BmpFile> ReadBmpAsync(string path, Action<double>? progress = null,
        double startProgress = 0.0, double progressRange = 1.0) {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var bmp = new BmpFile();

        // 读取文件头
        var fileHeaderSize = Marshal.SizeOf(typeof(BmpFileHeader));
        var fileHeaderBytes = new byte[fileHeaderSize];
        await file.ReadExactlyAsync(fileHeaderBytes, 0, fileHeaderSize);
        bmp.FileHeader = ByteArrayToStructure<BmpFileHeader>(fileHeaderBytes);

        progress?.Invoke(startProgress + progressRange * 0.2);

        // 读取信息头
        var infoHeaderSize = Marshal.SizeOf(typeof(BmpInfoHeader));
        var infoHeaderBytes = new byte[infoHeaderSize];
        await file.ReadExactlyAsync(infoHeaderBytes, 0, infoHeaderSize);
        bmp.InfoHeader = ByteArrayToStructure<BmpInfoHeader>(infoHeaderBytes);

        progress?.Invoke(startProgress + progressRange * 0.4);

        if (bmp.FileHeader.FileType != 0x4D42) {
            throw new Exception("不是BMP文件");
        }

        if (bmp.InfoHeader.BitCount != 24 && bmp.InfoHeader.BitCount != 32) {
            throw new Exception("本程序仅支持24/32位BMP文件");
        }

        // 读取图像数据
        file.Seek(bmp.FileHeader.OffsetData, SeekOrigin.Begin);
        var dataSize = bmp.FileHeader.FileSize - bmp.FileHeader.OffsetData;

        // 分块读取图像数据以避免阻塞
        const int bufferSize = 4096;
        var bytesRead = 0;
        bmp.ImageData = new List<byte>((int)dataSize);

        while (bytesRead < dataSize) {
            var bytesToRead = (int)Math.Min(bufferSize, dataSize - bytesRead);
            var buffer = new byte[bytesToRead];
            var read = await file.ReadAsync(buffer, 0, bytesToRead);
            if (read == 0) // 已经到达文件末尾
                break;
            bmp.ImageData.AddRange(buffer);
            bytesRead += read;

            // 更新进度
            progress?.Invoke(startProgress + progressRange * (0.4 + 0.6 * bytesRead / dataSize));
        }

        return bmp;
    }

    // 异步写入BMP文件
    private async Task WriteBmpAsync(string path, BmpFile bmp, Action<double>? progress = null,
        double startProgress = 0.0, double progressRange = 1.0) {
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        // 写入文件头
        var fileHeaderBytes = StructureToByteArray(bmp.FileHeader);
        await file.WriteAsync(fileHeaderBytes, 0, fileHeaderBytes.Length);

        progress?.Invoke(startProgress + progressRange * 0.2);

        // 写入信息头
        var infoHeaderBytes = StructureToByteArray(bmp.InfoHeader);
        await file.WriteAsync(infoHeaderBytes, 0, infoHeaderBytes.Length);

        progress?.Invoke(startProgress + progressRange * 0.4);

        // 分块写入图像数据以避免阻塞
        const int bufferSize = 4096;
        var totalBytes = bmp.ImageData.Count;
        var bytesWritten = 0;

        while (bytesWritten < totalBytes) {
            var bytesToWrite = Math.Min(bufferSize, totalBytes - bytesWritten);
            var buffer = new byte[bytesToWrite];
            bmp.ImageData.CopyTo(bytesWritten, buffer, 0, bytesToWrite);
            await file.WriteAsync(buffer, 0, buffer.Length);
            bytesWritten += bytesToWrite;

            // 更新进度
            progress?.Invoke(startProgress + progressRange * (0.4 + 0.6 * bytesWritten / totalBytes));
        }
    }

    // 异步读取要隐藏的文件
    private async Task<HideFile> ReadFileToHideAsync(string path, Action<double>? progress = null,
        double startProgress = 0.0, double progressRange = 1.0) {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

        var result = new HideFile {
            Size = file.Length
        };

        // 分块读取文件数据以避免阻塞
        const int bufferSize = 4096;
        var bytesRead = 0;
        result.Data = new List<byte>((int)file.Length);

        while (bytesRead < file.Length) {
            var bytesToRead = (int)Math.Min(bufferSize, file.Length - bytesRead);
            var buffer = new byte[bytesToRead];
            var read = await file.ReadAsync(buffer, 0, bytesToRead);
            if (read == 0)
                break;
            result.Data.AddRange(buffer);
            bytesRead += read;

            // 更新进度
            progress?.Invoke(startProgress + progressRange * bytesRead / file.Length);
        }


        return result;
    }

    // 创建数据头
    private HideDataHeader CreateDataHeader(string extension, uint fileSize) {
        var header = new HideDataHeader {
            Size = fileSize,
            // 设置ID
            Id = ['S', 'D', 'S', 'C', '0', '6', '2', '3'],
            // 设置文件名扩展
            Extension = new char[8]
        };

        if (!string.IsNullOrEmpty(extension)) {
            var copyLen = Math.Min(extension.Length, 7);
            extension[..copyLen].ToCharArray().CopyTo(header.Extension, 0);
        }

        return header;
    }

    // 结构体转二进制位
    private List<bool> StructToBits(HideDataHeader header) {
        var bytes = StructureToByteArray(header);
        return BytesToBits([..bytes]);
    }

    // 字节数组转二进制位
    private List<bool> BytesToBits(List<byte> data) {
        var bits = new List<bool>(data.Count * 8);

        foreach (var b in data) {
            for (var i = 0; i < 8; i++) {
                bits.Add(((b >> i) & 1) == 1);
            }
        }

        return bits;
    }

    // 计算可用空间（单位：位）
    private long CalculateAvailableSpace(BmpFile bmp) {
        var info = bmp.InfoHeader;
        return info.Width * info.Height * info.BitCount / 8;
    }

    // 异步嵌入数据到像素数据
    private async Task EmbedDataAsync(List<byte> imageData, List<bool> headerBits, List<bool> fileBits,
        Action<double>? progress = null, double startProgress = 0.0, double progressRange = 1.0) {
        var totalBits = headerBits.Count + fileBits.Count;
        var bitIndex = 0;

        const double targetTimeSeconds = 8;
        var dynamicMod = (int)Math.Max(100, totalBits / (80 * targetTimeSeconds));

        // 嵌入头部数据
        for (int i = 0; i < headerBits.Count; i++, bitIndex++) {
            imageData[bitIndex] = EmbedBit(imageData[bitIndex], headerBits[i]);

            // 每处理100位更新一次进度
            if (bitIndex % dynamicMod == 0 && progress != null) {
                progress.Invoke(startProgress + progressRange * bitIndex / totalBits);
                await Task.Delay(1); // 让出控制权，避免阻塞UI线程
            }
        }

        // 嵌入文件数据
        for (var i = 0; i < fileBits.Count; i++, bitIndex++) {
            imageData[bitIndex] = EmbedBit(imageData[bitIndex], fileBits[i]);

            // 每处理100位更新一次进度
            if (bitIndex % dynamicMod == 0 && progress != null) {
                progress.Invoke(startProgress + progressRange * bitIndex / totalBits);
                await Task.Delay(1); // 让出控制权，避免阻塞UI线程
            }
        }

        progress?.Invoke(startProgress + progressRange);
    }

    // 嵌入单个位到字节
    private byte EmbedBit(byte byteValue, bool bit) {
        // 0xFE = 11111110，清除最低位后，或上隐藏bit（0/1）
        return (byte)((byteValue & 0xFE) | (bit ? 1 : 0));
    }

    // 提取数据头
    private Tuple<HideDataHeader, int> ExtractDataHeader(List<byte> imageData) {
        var headerBits = new List<bool>(160);

        for (var i = 0; i < 160; i++) {
            headerBits.Add((imageData[i] & 0x01) == 1);
        }

        var headerBytes = BitsToBytes(headerBits);
        var header = ByteArrayToStructure<HideDataHeader>(headerBytes.ToArray());

        return new Tuple<HideDataHeader, int>(header, 160);
    }

    // 验证数据头有效性
    private bool ValidateDataHeader(HideDataHeader header) {
        char[] expectedId = ['S', 'D', 'S', 'C', '0', '6', '2', '3'];
        return header.Id.SequenceEqual(expectedId);
    }

    // 异步提取文件数据位
    private async Task<List<bool>> ExtractFileBitsAsync(List<byte> imageData, int startBit, uint fileSize,
        Action<double>? progress = null, double startProgress = 0.0, double progressRange = 1.0) {
        var bits = new List<bool>((int)fileSize * 8);
        var totalBits = fileSize * 8;

        const double targetTimeSeconds = 8;
        var dynamicMod = (int)Math.Max(100, totalBits / (80 * targetTimeSeconds));

        for (var i = startBit; i < startBit + totalBits; i++) {
            bits.Add((imageData[i] & 0x01) == 1);

            // 每处理一定位更新一次进度
            if ((i - startBit) % dynamicMod == 0 && progress != null) {
                progress.Invoke(startProgress + progressRange * (i - startBit) / totalBits);
                await Task.Delay(1); // 让出控制权，避免阻塞UI线程
            }
        }

        progress?.Invoke(startProgress + progressRange);

        return bits;
    }

    // 二进制位转字节数组
    private List<byte> BitsToBytes(List<bool> bits) {
        var bytes = new List<byte>(bits.Count / 8 + 1);

        for (var i = 0; i < bits.Count; i += 8) {
            byte byteValue = 0;
            for (var j = 0; j < 8 && i + j < bits.Count; j++) {
                if (bits[i + j]) {
                    byteValue |= (byte)(1 << j);
                }
            }

            bytes.Add(byteValue);
        }

        return bytes;
    }

    // 异步写入提取的文件
    private async Task WriteExtractedFileAsync(string path, char[] name, List<byte> data,
        Action<double>? progress = null, double startProgress = 0.0, double progressRange = 1.0) {
        var extension = new string(name).TrimEnd('\0');
        var fullPath = path;

        // 如果路径没有扩展名，则添加提取的扩展名
        if (string.IsNullOrEmpty(Path.GetExtension(path)) && !string.IsNullOrEmpty(extension)) {
            fullPath = path + "." + extension;
        }

        // 如果文件已存在，添加数字后缀
        fullPath = GetUniqueFileName(fullPath);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        // 分块写入文件数据以避免阻塞
        var bufferSize = 4096;
        var totalBytes = data.Count;
        var bytesWritten = 0;

        while (bytesWritten < totalBytes) {
            var bytesToWrite = Math.Min(bufferSize, totalBytes - bytesWritten);
            var buffer = new byte[bytesToWrite];
            data.CopyTo(bytesWritten, buffer, 0, bytesToWrite);
            await file.WriteAsync(buffer, 0, buffer.Length);
            bytesWritten += bytesToWrite;

            // 更新进度
            progress?.Invoke(startProgress + progressRange * bytesWritten / totalBytes);
        }
    }

    // 获取文件扩展名
    private string GetFileExtension(string path) {
        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) ? ext.TrimStart('.') : "";
    }

    // 获取唯一的文件名（如果文件已存在，添加数字后缀）
    private string GetUniqueFileName(string filePath) {
        if (!File.Exists(filePath)) {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var counter = 1;
        var newFilePath = Path.Combine(directory, $"{fileName}_{counter}{extension}");

        while (File.Exists(newFilePath)) {
            counter++;
            newFilePath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
        }

        return newFilePath;
    }

    // 辅助方法：将结构体转换为字节数组
    private byte[] StructureToByteArray<T>(T structure) where T : struct {
        var size = Marshal.SizeOf(structure);
        var byteArray = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);

        try {
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, byteArray, 0, size);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }

        return byteArray;
    }

    // 辅助方法：将字节数组转换为结构体
    private T ByteArrayToStructure<T>(byte[] byteArray) where T : struct {
        if (byteArray == null) {
            throw new ArgumentNullException(nameof(byteArray));
        }

        var size = Marshal.SizeOf(typeof(T));
        if (byteArray.Length < size) {
            throw new ArgumentException($"Byte array is too small. Expected: {size}, Actual: {byteArray.Length}");
        }

        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(byteArray, 0, ptr, size);

        try {
            return Marshal.PtrToStructure<T>(ptr);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
    }
}