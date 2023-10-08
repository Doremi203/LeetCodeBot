using LeetCodeBot.Dal.Repositories.Interfaces;

namespace LeetCodeBot.Dal.Repositories;

public class RegisteredUsersRepository : IRegisteredUsersRepository
{
    private readonly List<long> _registeredUsers = new();
    
    public void Add(long userId)
    {
        _registeredUsers.Add(userId);
    }

    public void Remove(long userId)
    {
        _registeredUsers.Remove(userId);
    }

    public bool Contains(long userId)
    {
        return _registeredUsers.Contains(userId);
    }

    public IEnumerable<long> GetAll()
    {
        return _registeredUsers.ToArray();
    }
}