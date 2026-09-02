using Squash.Core.Services;
using Squash.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Squash.Core.Tests;

[TestClass]
public sealed class EncodeServiceTests
{
    [TestMethod]
    public void ExactTargetIsWithinZeroTolerance()
    {
        Assert.IsTrue(EncodeService.IsWithinTolerance(10_000, 10_000, 0));
    }

    [TestMethod]
    public void FileAtToleranceBoundaryIsAccepted()
    {
        Assert.IsTrue(EncodeService.IsWithinTolerance(9_800, 10_000, 2));
    }

    [TestMethod]
    public void FileBelowToleranceBoundaryIsRejected()
    {
        Assert.IsFalse(EncodeService.IsWithinTolerance(9_799, 10_000, 2));
    }

    [TestMethod]
    public void OverTargetFileIsRejected()
    {
        Assert.IsFalse(EncodeService.IsWithinTolerance(10_001, 10_000, 2));
    }

    [TestMethod]
    public void HdrTransferSelectsTenBitOutput()
    {
        var video = CreateVideo(pixelFormat: "yuv420p", colorTransfer: "smpte2084");

        Assert.IsTrue(EncodeService.ShouldUseTenBit(video));
    }

    [TestMethod]
    public void TenBitPixelFormatSelectsTenBitOutput()
    {
        var video = CreateVideo(pixelFormat: "yuv420p10le");

        Assert.IsTrue(EncodeService.ShouldUseTenBit(video));
    }

    [TestMethod]
    public void MissingAudioUsesNoAudioBudget()
    {
        var media = CreateMedia();

        var plan = EncodeService.SelectAudioPlan(media, 10_000_000, 0);

        Assert.AreEqual(EncodeService.AudioMode.None, plan.Mode);
        Assert.AreEqual(0, plan.EstimatedBitrateKbps);
        Assert.AreEqual(0, plan.FixedBytes);
    }

    [TestMethod]
    public void EveryPreservedAudioStreamUsesCopyMode()
    {
        var media = CreateMedia(
            CreateAudio(1, "aac", 2, 96),
            CreateAudio(2, "ac3", 6, 384));

        var plan = EncodeService.SelectAudioPlan(media, 10_000_000, 3_000_000);

        Assert.AreEqual(EncodeService.AudioMode.Copy, plan.Mode);
        Assert.AreEqual(3_000_000, plan.FixedBytes);
        Assert.AreEqual(400, plan.EstimatedBitrateKbps);
    }

    [TestMethod]
    public void PreservedAudioThatCannotFitThrows()
    {
        var media = CreateMedia(CreateAudio(1, "flac", 2, 900));

        Assert.Throws<UnableToReachTargetSizeException>(
            () => EncodeService.SelectAudioPlan(media, 1_000_000, 900_000));
    }

    [TestMethod]
    public void H264UsesFastFirstPass()
    {
        var settings = EncodeService.GetEncodeSettings(1, CreateMedia()).ToArray();

        var optionIndex = Array.IndexOf(settings, "-fastfirstpass");

        Assert.IsGreaterThanOrEqualTo(0, optionIndex);
        Assert.AreEqual("1", settings[optionIndex + 1]);
    }

    [TestMethod]
    public void X265PreservesTwelveBitFourFourFour()
    {
        var video = CreateVideo(pixelFormat: "yuv444p12le");

        var pixelFormat = EncodeService.SelectOutputPixelFormat("libx265", video);

        Assert.AreEqual("yuv444p12le", pixelFormat);
    }

    [TestMethod]
    public void SvtAv1UsesCompatibleTenBitFourTwoZero()
    {
        var video = CreateVideo(pixelFormat: "yuv444p12le");

        var pixelFormat = EncodeService.SelectOutputPixelFormat("libsvtav1", video);

        Assert.AreEqual("yuv420p10le", pixelFormat);
    }

    private static EncodeService.MediaInfo CreateMedia(params EncodeService.AudioStreamInfo[] audioStreams) =>
        new(60, 2_000, CreateVideo(), audioStreams);

    private static EncodeService.AudioStreamInfo CreateAudio(
        int index,
        string codecName,
        int channels,
        double bitrateKbps) =>
        new(index, codecName, channels, channels == 1 ? "mono" : "stereo", bitrateKbps);

    private static EncodeService.VideoStreamInfo CreateVideo(
        string pixelFormat = "yuv420p",
        string? colorTransfer = null) =>
        new(
            CodecName: "h264",
            Width: 1920,
            Height: 1080,
            PixelFormat: pixelFormat,
            FrameRate: "60/1",
            ColorRange: "tv",
            ColorSpace: "bt709",
            ColorTransfer: colorTransfer,
            ColorPrimaries: "bt709",
            ChromaLocation: "left",
            BitrateKbps: 1_900);
}
