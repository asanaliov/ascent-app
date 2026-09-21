using ascent_app.Models;

namespace ascent_app.Services;

public record TrailFact(string Label, string Value, string Icon);
public record TrailTip(string Text, string Icon);
public record TrailAdvice(IReadOnlyList<TrailFact> Facts, IReadOnlyList<TrailTip> Tips);

public interface ITrailAdviceService
{
    TrailAdvice For(Trail trail);
}

public class TrailAdviceService : ITrailAdviceService
{
    private readonly IDifficultyService _difficulty;

    public TrailAdviceService(IDifficultyService difficulty)
    {
        _difficulty = difficulty;
    }

    public TrailAdvice For(Trail trail)
    {
        var km = trail.DistanceKm;
        var gain = trail.ElevationGainM;
        var score = _difficulty.Score(km, gain);
        var tags = trail.TrailTags
            .Select(tt => tt.Tag?.Name ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var text = $"{trail.ShortDescription} {trail.Description}".ToLowerInvariant();
        var oneWay = tags.Contains("Point to Point") || text.Contains("one way") || text.Contains("one-way") || text.Contains("point-to-point");
        var exposed = gain >= 800 || tags.Overlaps(new[] { "Alpine", "Peak", "Ridge", "Panorama" });

        var facts = new List<TrailFact>
        {
            new("Difficulty score", $"{score:0}  (√ 2 × {gain:N0} m × {km:0.#} km)", "ti-math-function"),
            new("Climb per km", km > 0 ? $"{gain / km:0} m" : "—", "ti-trending-up"),
            new("Route type", oneWay ? "One way" : "Out and back", oneWay ? "ti-arrow-right" : "ti-arrows-left-right"),
            new("Walking pace", trail.EstimatedTimeHours > 0 ? $"{km / trail.EstimatedTimeHours:0.0} km/h with breaks" : "—", "ti-walk"),
        };

        var tips = new List<TrailTip>();
        if (score >= 170)
            tips.Add(new("Start at first light and plan for the full estimated time plus a margin. This is a full mountain day.", "ti-sunrise"));
        else if (score >= 100)
            tips.Add(new("Allow a full half-day, and turn back if you are behind schedule at the halfway point.", "ti-clock"));

        if (gain >= 1000)
            tips.Add(new("Expect it roughly 6 °C colder at the top than at the trailhead. Pack a windproof layer and a hat even in summer.", "ti-temperature"));
        else if (gain >= 500)
            tips.Add(new("Pack a light extra layer. It is noticeably cooler and windier at the top.", "ti-shirt"));

        if (km >= 15)
            tips.Add(new("Carry at least 2 litres of water per person. Springs along the route cannot be relied on.", "ti-droplet"));
        else if (km >= 6)
            tips.Add(new("Carry at least 1 litre of water per person.", "ti-droplet"));

        if (exposed)
            tips.Add(new("Above the tree line there is no shelter from sun or storms. Check the forecast and leave the summit if thunder builds.", "ti-cloud-storm"));

        if (text.Contains("border"))
            tips.Add(new("The summit sits on the state border. Carry ID and check current crossing rules before you go.", "ti-id"));

        if (text.Contains("fog") || text.Contains("cloud") || text.Contains("navigation") || text.Contains("marking"))
            tips.Add(new("Carry an offline map or a downloaded track. Marking is patchy and cloud can hide the way.", "ti-map-2"));

        if (tags.Overlaps(new[] { "Waterfall", "Water", "Canyon", "Lake" }) || text.Contains("slippery"))
            tips.Add(new("Rock and steps near water are slippery after rain. Wear shoes with grip.", "ti-shoe"));

        if (oneWay)
            tips.Add(new("This route is one way. Arrange a pickup or a second car at the far end.", "ti-car"));

        if (gain >= 800)
            tips.Add(new("Best from June to October. Snow lingers on the upper slopes into late spring.", "ti-snowflake"));

        if (tags.Overlaps(new[] { "Monastery", "History", "Archaeology" }))
            tips.Add(new("Dress modestly at the monastery or site, and leave stones and ruins as you find them.", "ti-building-church"));

        if (score < 50 || tags.Contains("Family"))
            tips.Add(new("Suitable for children and first-time hikers. Sturdy shoes and a snack are still worth packing.", "ti-mood-smile"));

        return new TrailAdvice(facts, tips.Take(5).ToList());
    }
}
