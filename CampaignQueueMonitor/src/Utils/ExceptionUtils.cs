namespace CampaignQueueMonitor.Utils;

public class ExceptionUtils
{
    public static T SuppressExceptions<T>(Func<T> func, T defaultValue)
    {
        try
        {
            return func();
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
}
