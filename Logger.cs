namespace VocalKnight
{
    public static class Logger
    {
        public static void Log(object obj) => VocalKnight.Instance.Log(obj);
        
        public static void LogWarn(object obj) => VocalKnight.Instance.LogWarn(obj);
        
        public static void LogError(object obj) => VocalKnight.Instance.LogError(obj);
    }
}