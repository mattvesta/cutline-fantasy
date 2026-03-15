namespace Cutline.Core.Entities;

/// <summary>
/// Carries cross-platform ID values to be patched onto an existing Player record.
/// Only non-null fields are written; nulls leave the existing value untouched.
/// </summary>
public record PlayerIdPatch(
    string GsisId,
    string? EspnId,
    string? SleeperId,
    string? YahooId);
