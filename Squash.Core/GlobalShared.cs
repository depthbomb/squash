namespace Squash.Core;

public static class GlobalShared
{
    public const string MutexName = "Squash";

    public static class Product
    {
        public const string AppName        = MutexName;
        public const string Organization   = "Caprine Logic";
        public const string AppUserModelId = "CaprineLogic.Squash";
    }
}
