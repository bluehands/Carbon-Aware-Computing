using System.Text;
using CarbonAware.Model;

namespace CarbonAwareComputing.ExecutionForecast.Test
{
    public abstract class ExecutionForecastContextSpecification : ContextSpecification
    {
        protected CarbonAwareDataProviderWithCustomForecast m_Provider = null!;
        protected string m_ContentFile = null!;
        protected ComputingLocation m_Location = null!;
        protected DateTimeOffset m_Now;
        protected GridCarbonIntensity m_Intensity = null!;

        protected override void Given()
        {
            m_Provider = new CarbonAwareDataProviderWithCustomForecast(GetEmissionData);
            base.Given();
        }

        protected override void When()
        {
            m_Intensity = m_Provider.GetCarbonIntensity(m_Location, m_Now).GetAwaiter().GetResult();
        }

        private Task<CachedData> GetEmissionData(ComputingLocation computingLocation, CachedData cachedData)
        {
            var jsonFile = System.Text.Json.JsonSerializer.Deserialize<EmissionsForecastJsonFile>(m_ContentFile)!;
            var emissionsData = jsonFile.Emissions.Select(e => new EmissionsData()
            {
                Duration = e.Duration,
                Rating = e.Rating,
                Location = m_Location.Name,
                Time = e.Time
            }).ToList();
            return Task.FromResult(new CachedData(emissionsData, DateTimeOffset.Now, Guid.NewGuid().GetHashCode().ToString()));
        }

    }

    [TestClass]
    public class Given_a_forecast_file_for_de : ExecutionForecastContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.de);
            m_Location = ComputingLocations.Germany;
            m_Now = new DateTimeOffset(2024, 3, 24, 9, 34, 3, TimeSpan.FromHours(1));
            base.Given();
        }

        [TestMethod]
        public void Then_the_intensity_is_provided()
        {
            Assert.IsTrue(m_Intensity is GridCarbonIntensity.EmissionData_ { Value: 151.2 });
        }
    }

    [TestClass]
    public class Given_a_forecast_file_for_de_out_of_boundary : ExecutionForecastContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.de);
            m_Location = ComputingLocations.Germany;
            m_Now = new DateTimeOffset(2023, 3, 24, 9, 34, 3, TimeSpan.FromHours(1));
            base.Given();
        }

        [TestMethod]
        public void Then_the_intensity_is_not_provided()
        {
            Assert.IsTrue(m_Intensity is GridCarbonIntensity.NoData_);
        }
    }

    [TestClass]
    public class Given_a_forecast_file_for_de_on_first_point_in_time : ExecutionForecastContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.de);
            m_Location = ComputingLocations.Germany;
            m_Now = new DateTimeOffset(2024, 3, 24, 0, 0, 0, TimeSpan.Zero);
            base.Given();
        }

        [TestMethod]
        public void Then_the_intensity_is_not_provided()
        {
            Assert.IsTrue(m_Intensity is GridCarbonIntensity.EmissionData_ { Value: 185.3 });
        }
    }

    [TestClass]
    public class Given_a_forecast_file_for_de_on_last_point_in_time : ExecutionForecastContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.de);
            m_Location = ComputingLocations.Germany;
            m_Now = new DateTimeOffset(2024, 3, 25, 22, 45, 0, TimeSpan.Zero);
            base.Given();
        }

        [TestMethod]
        public void Then_the_intensity_is_not_provided()
        {
            Assert.IsTrue(m_Intensity is GridCarbonIntensity.EmissionData_ { Value: 0.0 });
        }
    }

    [TestClass]
    public class CalculateBestExecutionTimeSpecs
    {
        static readonly ComputingLocation Location = ComputingLocations.Germany;
        static readonly DateTimeOffset DataStart = new(2025, 03, 14, 0, 0, 0, TimeSpan.Zero);
        static readonly double[] FiveMinuteTestData = [10, 20, 30, 40, 5];
        static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);

        [TestMethod]
        [DynamicData(nameof(FiveMinuteEmissionDataCases))]
        public async Task FiveMinuteEmissionData(
            DateTimeOffset earliestExecutionTime, 
            DateTimeOffset latestExecutionTime, 
            TimeSpan estimatedJobDuration,
            ExecutionTime expectedBestExecutionTime)
        {
            var interval = FiveMinutes;
            var provider = CreateProvider(FiveMinuteTestData, interval);

            var bestExecution = await provider.CalculateBestExecutionTime(Location, earliestExecutionTime, latestExecutionTime, estimatedJobDuration);
            expectedBestExecutionTime
                .Switch(
                    noForecast: n => Assert.AreEqual(n, bestExecution),
                    bestExecutionTime: expected =>
                    {
                        var actual = bestExecution as ExecutionTime.BestExecutionTime_;
                        Assert.IsNotNull(actual);
                        Assert.AreEqual(expected.ExecutionTime, actual.ExecutionTime);
                        Assert.AreEqual(expected.CarbonIntensity, actual.CarbonIntensity);
                        Assert.AreEqual(expected.Duration, actual.Duration);
                    });

        }

        static IEnumerable<object[]> FiveMinuteEmissionDataCases =>
        [
            TestRow(
                earliestExecutionTime: DataStart.AddMinutes(10), 
                latestExecutionTime: DataStart.AddMinutes(20),
                estimatedJobDuration: TimeSpan.FromMinutes(10), 
                expectedBestExecutionTime: DataStart.AddMinutes(15), 
                expectedCarbonIntensity: 22.5
            )
        ];

        static object[] TestRow(
            DateTimeOffset earliestExecutionTime, 
            DateTimeOffset latestExecutionTime, 
            TimeSpan estimatedJobDuration,
            DateTimeOffset expectedBestExecutionTime,
            double expectedCarbonIntensity) =>
        [
            earliestExecutionTime,
            latestExecutionTime,
            estimatedJobDuration,
            ExecutionTime.BestExecutionTime(
                expectedBestExecutionTime,
                estimatedJobDuration < FiveMinutes ? FiveMinutes : estimatedJobDuration,
                expectedCarbonIntensity,
                0
            )
        ];

        static CarbonAwareDataProviderWithCustomForecast CreateProvider(double[] intensities, TimeSpan interval)
        {
            var emissionData = EmissionData(intensities, interval);
            var provider = new CarbonAwareDataProviderWithCustomForecast((_, _) => Task.FromResult(new CachedData(emissionData, DateTimeOffset.Now, Guid.NewGuid().GetHashCode().ToString())));
            return provider;
        }

        

        static List<EmissionsData> EmissionData(double[] intensities, TimeSpan interval)
        {
            return intensities.Select((d, i) => new EmissionsData()
            {
                Duration = interval,
                Location = Location.Name,
                Rating = d,
                Time = DataStart.Add(i * interval)
            }).ToList();
        }
    }
}