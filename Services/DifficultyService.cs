using ascent_app.Data;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Services;

public interface IDifficultyService
{
    double Score(double distanceKm, int elevationGainM);
    string GetLabel(double distanceKm, int elevationGainM);
    Task<int> ResolveDifficultyIdAsync(double distanceKm, int elevationGainM);
}

public class DifficultyService : IDifficultyService
{
    private readonly AscentDbContext _context;

    public DifficultyService(AscentDbContext context)
    {
        _context = context;
    }

    public double Score(double distanceKm, int elevationGainM)
        => Math.Sqrt(2 * elevationGainM * distanceKm);

    public string GetLabel(double distanceKm, int elevationGainM)
        => Score(distanceKm, elevationGainM) switch
        {
            < 50 => "Easy",
            < 100 => "Moderate",
            < 150 => "Hard",
            _ => "Strenuous",
        };

    public async Task<int> ResolveDifficultyIdAsync(double distanceKm, int elevationGainM)
    {
        var label = GetLabel(distanceKm, elevationGainM);
        var difficulty = await _context.Difficulties
            .FirstOrDefaultAsync(d => d.Label == label);

        if (difficulty is null)
            throw new InvalidOperationException(
                $"Difficulty '{label}' is not seeded. Run the DbInitializer.");

        return difficulty.Id;
    }
}
