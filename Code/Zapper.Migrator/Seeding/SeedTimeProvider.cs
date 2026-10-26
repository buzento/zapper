namespace Zapper.Migrator.Seeding;

// each seeded payment is moved by its own domain methods at the time its row in the plan states, so the seed sets
// the time before each step, D-12
public sealed class SeedTimeProvider : TimeProvider
{
    private DateTimeOffset now;

    public override DateTimeOffset GetUtcNow() => now;

    public void SetUtcNow(DateTimeOffset value) => now = value.ToUniversalTime();
}
