using System.Transactions;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public abstract class BaseRepository
{
    protected readonly IOptionsSnapshot<DalOptions> DalSettings;

    protected BaseRepository(IOptionsSnapshot<DalOptions> dalSettings)
    {
        DalSettings = dalSettings;
    }
}