using System.Security.Cryptography;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

internal static class SequentialGuid
{
    private const int RandomByteCount = 8;
    private const int HighShift = 32;
    private const int MiddleShift = 16;
    private const long LowMask = 0xFFFF;

    private static long _last;

    public static Guid Next()
    {
        long value = NextValue();
        byte[] random = RandomNumberGenerator.GetBytes(RandomByteCount);
        return new Guid((int)(value >> HighShift), (short)(value >> MiddleShift), (short)(value & LowMask), random);
    }

    private static long NextValue()
    {
        long last = Interlocked.Read(ref _last);
        long next = Math.Max(DateTime.UtcNow.Ticks, last + 1);
        return Interlocked.CompareExchange(ref _last, next, last) == last ? next : NextValue();
    }
}
