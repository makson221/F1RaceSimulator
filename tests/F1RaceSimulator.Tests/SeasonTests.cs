using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.Tests;

/// <summary>Тести даних сезону, чемпіонату (Singleton) і допоміжних класів.</summary>
public class SeasonTests
{
    [Fact]
    public void SeasonData_Has11Teams22Drivers23Tracks()
    {
        var data = TestHelpers.Season;
        Assert.Equal(11, data.Teams.Count);
        Assert.Equal(22, data.Drivers.Count);
        Assert.Equal(23, data.Tracks.Count);
    }

    [Fact]
    public void EveryTeam_HasTwoDrivers()
    {
        var data = TestHelpers.Season;
        Assert.All(data.Teams, team => Assert.Equal(2, data.DriversOf(team).Count()));
    }

    [Fact]
    public void Tracks_AreInCalendarOrder()
    {
        var rounds = TestHelpers.Season.Tracks.Select(t => t.Round).ToList();
        Assert.Equal(Enumerable.Range(1, 23), rounds);
    }

    [Fact]
    public void Monza_HasRealLengthAndLaps()
    {
        var monza = TestHelpers.Season.Tracks.Single(t => t.Id == "it-1922");
        Assert.Equal(5.793, monza.LengthKm, precision: 3);
        Assert.Equal(53, monza.Laps);
    }

    [Fact]
    public void NextTrack_ReturnsFirstUpcomingRace()
    {
        var next = TestHelpers.Season.NextTrack(new DateOnly(2026, 10, 9));
        Assert.Equal("sg-2008", next.Id);     // Сінгапур, 11 жовтня
    }

    [Fact]
    public void LoadSeason_ThrowsForMissingFolder()
    {
        Assert.Throws<DirectoryNotFoundException>(() => DataLoader.LoadSeason("/no/such/folder"));
    }

    [Fact]
    public void Championship_IsSingleton()
    {
        Assert.Same(Championship.Instance, Championship.Instance);
    }

    [Fact]
    public void Championship_AddsPointsAndSavesToFile()
    {
        var championship = Championship.Instance;
        championship.Reset();

        var data = TestHelpers.Season;
        var track = data.Tracks[0];
        var race = new Race(track, CarFactory.CreateCars(data), new SystemRandomSource(9), 0);
        race.RunToFinish();
        championship.AddRaceResult(track, race.GetResults());

        var winner = race.GetResults()[0].Car.Driver.Code;
        Assert.Equal(25, championship.PointsOf(winner));
        Assert.Equal(winner, championship.GetDriverStandings()[0].Code);

        string path = Path.Combine(Path.GetTempPath(), $"championship-test-{Guid.NewGuid():N}.json");
        try
        {
            championship.Save(path);
            championship.Reset();
            Assert.Empty(championship.Races);

            championship.Load(path);
            Assert.Single(championship.Races);
            Assert.Equal(25, championship.PointsOf(winner));
        }
        finally
        {
            File.Delete(path);
            championship.Reset();
        }
    }

    [Fact]
    public void Championship_ReplacesResultOfSameRound()
    {
        var championship = Championship.Instance;
        championship.Reset();
        var data = TestHelpers.Season;
        var track = data.Tracks[1];

        for (int seed = 1; seed <= 2; seed++)
        {
            var race = new Race(track, CarFactory.CreateCars(data), new SystemRandomSource(seed), 0);
            race.RunToFinish();
            championship.AddRaceResult(track, race.GetResults());
        }

        Assert.Single(championship.Races);
        Assert.Equal(101, championship.GetDriverStandings().Sum(r => r.Points));
        championship.Reset();
    }

    [Fact]
    public void Championship_AccumulatesPointsAfterEveryGrandPrix()
    {
        var championship = Championship.Instance;
        championship.Reset();
        var data = TestHelpers.Season;

        // Два етапи поспіль: очки підсумовуються, а таблицю можна подивитися «після 1-го етапу».
        var first = data.Tracks[0];
        var second = data.NextTrackAfter(first)!;
        Assert.Equal(first.Round + 1, second.Round);

        foreach (var (track, seed) in new[] { (first, 11), (second, 12) })
        {
            var race = new Race(track, CarFactory.CreateCars(data), new SystemRandomSource(seed), 0);
            race.RunToCompletion();
            Assert.True(race.IsCompleted);
            championship.AddRaceResult(track, race.GetResults());
        }

        Assert.Equal(2, championship.Races.Count);
        Assert.Equal(2 * 101, championship.GetDriverStandings().Sum(r => r.Points));
        Assert.Equal(101, championship.GetDriverStandings(first.Round).Sum(r => r.Points));

        var leader = championship.GetDriverStandings()[0];
        int secondRacePoints = championship.Races[1].Results.Single(e => e.DriverCode == leader.Code).Points;
        Assert.Equal(leader.Points - secondRacePoints, championship.PointsBefore(leader.Code, second.Round));
        Assert.True(championship.HasRound(second.Round));
        Assert.Equal(data.Teams.Count, championship.GetTeamStandings().Count);

        championship.Reset();
    }

    [Fact]
    public void Calendar_NextTrackAfterLastRoundIsNull()
    {
        var data = TestHelpers.Season;
        Assert.Null(data.NextTrackAfter(data.Tracks[^1]));
        Assert.Same(data.Tracks[1], data.NextTrackAfter(data.Tracks[0]));
    }

    [Theory]
    [InlineData(1, 25)]
    [InlineData(3, 15)]
    [InlineData(10, 1)]
    [InlineData(11, 0)]
    [InlineData(0, 0)]
    public void PointsSystem_MatchesFormula1(int position, int points)
    {
        Assert.Equal(points, PointsSystem.ForPosition(position));
    }

    [Theory]
    [InlineData(1, "1 коло")]
    [InlineData(2, "2 кола")]
    [InlineData(5, "5 кіл")]
    [InlineData(11, "11 кіл")]
    [InlineData(21, "21 коло")]
    [InlineData(53, "53 кола")]
    public void UkrainianText_DeclinesLaps(int laps, string expected)
    {
        Assert.Equal(expected, UkrainianText.Laps(laps));
    }

    [Theory]
    [InlineData(1, "1 очко")]
    [InlineData(4, "4 очки")]
    [InlineData(12, "12 очок")]
    [InlineData(25, "25 очок")]
    [InlineData(101, "101 очко")]
    public void UkrainianText_DeclinesPoints(int points, string expected)
    {
        Assert.Equal(expected, UkrainianText.Points(points));
    }
}
