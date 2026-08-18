using Microsoft.VisualStudio.TestTools.UnitTesting;
using Squash.Core.Services;

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
        var media = CreateMedia(audio: null);

        var plan = EncodeService.SelectAudioPlan(media, 10 * 1024L * 1024L);

        Assert.AreEqual(EncodeService.AudioMode.None, plan.Mode);
        Assert.AreEqual(0, plan.EstimatedBitrateKbps);
    }

    [TestMethod]
    public void CompatibleAacAudioIsCopiedWhenItFits()
    {
        var media = CreateMedia(new EncodeService.AudioStreamInfo("aac", 2, 96));

        var plan = EncodeService.SelectAudioPlan(media, 10 * 1024L * 1024L);

        Assert.AreEqual(EncodeService.AudioMode.Copy, plan.Mode);
        Assert.AreEqual(96, plan.EstimatedBitrateKbps);
    }

    [TestMethod]
    public void HighBitrateMonoAudioIsReducedToMonoBudget()
    {
        var media = CreateMedia(new EncodeService.AudioStreamInfo("aac", 1, 192));

        var plan = EncodeService.SelectAudioPlan(media, 10 * 1024L * 1024L);

        Assert.AreEqual(EncodeService.AudioMode.Encode, plan.Mode);
        Assert.AreEqual(64, plan.EstimatedBitrateKbps);
    }

    private static EncodeService.MediaInfo CreateMedia(EncodeService.AudioStreamInfo? audio) =>
        new(60, 2_000, CreateVideo(), audio);

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
            BitrateKbps: 1_900);
}
