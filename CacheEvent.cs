/// <summary>
/// Cache Event class to manage cache events
/// </summary>
public class CacheEvent : EventArgs
{
    public CacheEventType EventType { get; }
    public string Key { get; }
    public object Value { get; }

    /// <summary>
    /// Cache evenet constructor
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public CacheEvent(CacheEventType eventType, string key, object value)
    {
        EventType = eventType;
        Key = key;
        Value = value;
    }
}

/// <summary>
/// Cache event Type Enumerations
/// </summary>
public enum CacheEventType
{
    Init,
    Add,
    Update,
    Remove,
    Get,
    Clear,
    Dispose
}
