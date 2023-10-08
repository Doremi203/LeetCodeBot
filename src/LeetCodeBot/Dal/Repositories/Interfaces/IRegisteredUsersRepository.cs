namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface IRegisteredUsersRepository
{
    void Add(long userId);

    void Remove(long userId);

    bool Contains(long userId);

    IEnumerable<long> GetAll();
}