namespace SendReceive;

sealed class Envelop<TContent> where TContent : class, new()
{
    public Dictionary<string, string> Headers { get; set; } = new();

    public Guid Id { get; set; }

    public TContent Content { get; set; }
}