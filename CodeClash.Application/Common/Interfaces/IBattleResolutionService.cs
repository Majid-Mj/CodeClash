using System;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces;

public interface IBattleResolutionService
{
    Task ResolveBattleAsync(Guid battleId, Guid winnerId, string language);
}
