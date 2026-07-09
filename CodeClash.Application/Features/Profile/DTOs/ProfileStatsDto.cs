using System;
using System.Collections.Generic;

namespace CodeClash.Application.Features.Profile.DTOs;

public record LanguagePreferenceDto(
    string Name,
    int Pct,
    string Color
);

public record BattleRecordDto(
    string Opponent,
    string Problem,
    string Result,
    int Score,
    string Language,
    string Duration,
    string Date,
    int EloChange
);

public record ProfileStatsDto(
    int TotalBattles,
    int Wins,
    string WinRate,
    int ProblemsSolved,
    string BestStreak,
    List<LanguagePreferenceDto> TopLanguages,
    List<BattleRecordDto> MatchHistory
);
