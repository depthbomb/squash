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
}
