using System;

public static class ConsoleManager
{
    public static event Action<string> OnOutputReceived;

    public static void AddOutput(string text)
    {
        OnOutputReceived?.Invoke(text);
    }
}
