namespace LeetCodeBot.Dal.Settings;

public record DalOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}