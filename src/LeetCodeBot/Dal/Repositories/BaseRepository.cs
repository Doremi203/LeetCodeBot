using System.Transactions;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public abstract class BaseRepository
{
    protected readonly DalOptions DalSettings;

    protected BaseRepository(DalOptions dalSettings)
    {
        DalSettings = dalSettings;
    }
}