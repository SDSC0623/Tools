namespace Tools.Services.IServices;

public interface IBmpSteganographyService {
    /// <summary>
    /// 隐藏文件到BMP图片中
    /// </summary>
    Task Hide(string bmpPath, string fileToHide, string outputFolderPath, Action<double>? updateProgressPercent = null,
        Action<string>? updateProgressText = null, Action<string>? notifyOutputBmpPath = null);

    /// <summary>
    /// 从BMP图片提取隐藏文件
    /// </summary>
    Task Extract(string bmpPath, string outputFolderPath, Action<double>? updateProgressPercent = null,
        Action<string>? updateProgressText = null);

    /// <summary>
    /// 检验数据头的校验码是否是SDSC0623
    /// </summary>
    Task<bool> Verify(string bmpPath, Action<double>? updateProgressPercent = null,
        Action<string>? updateProgressText = null);

    /// <summary>
    /// 获取最大可隐写文件大小（字节）
    /// </summary>
    Task<long> GetMaxHideSize(string bmpPath);
}