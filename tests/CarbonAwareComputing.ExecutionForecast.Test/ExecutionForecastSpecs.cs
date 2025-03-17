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
        static readonly double[] FiveMinuteTestData = [/*0*/10, /*5*/20, /*10*/30, /*15*/50, /*20*/5, /*25*/15, /*30*/100, /*35*/10];
        static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);

        [TestMethod]
        [DynamicData(nameof(FiveMinuteEmissionDataCases))]
        public async Task FiveMinuteEmissionData(
            string testCase,
            DateTimeOffset earliestExecutionTime, 
            DateTimeOffset latestExecutionTime, 
            TimeSpan estimatedJobDuration,
            ExecutionTime expectedBestExecutionTime)
        {
            var provider = CreateProvider(FiveMinuteTestData, FiveMinutes);

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
            TestRowNoForecast(
                "Execution range before data start",
                earliestExecutionTime: DataStart.AddMinutes(-40),
                latestExecutionTime: DataStart.AddMinutes(9),
                estimatedJobDuration: TimeSpan.FromMinutes(10)
            ),
            TestRow(
                "Execution range starts before data start with single possible execution time",
                earliestExecutionTime: DataStart.AddMinutes(-40),
                latestExecutionTime: DataStart.AddMinutes(10),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(0),
                expectedCarbonIntensity: 15
            ),
            TestRow(
                "Execution range starts before data start",
                earliestExecutionTime: DataStart.AddMinutes(-20),
                latestExecutionTime: DataStart.AddMinutes(20),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(0),
                expectedCarbonIntensity: 15
            ),
            TestRow(
                "Execution range within data interval",
                earliestExecutionTime: DataStart.AddMinutes(5),
                latestExecutionTime: DataStart.AddMinutes(25),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(5),
                expectedCarbonIntensity: 25
            ),
            TestRow(
                "Execution range greater than data interval",
                earliestExecutionTime: DataStart.AddMinutes(-40),
                latestExecutionTime: DataStart.AddMinutes(120),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(20),
                expectedCarbonIntensity: 10
            ),
            TestRow(
                "Execution starts within, end after data interval",
                earliestExecutionTime: DataStart.AddMinutes(25),
                latestExecutionTime: DataStart.AddMinutes(120),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(30),
                expectedCarbonIntensity: 55
            ),
            TestRow(
                "Execution starts within, end after data interval, start between data points",
                earliestExecutionTime: DataStart.AddMinutes(21),
                latestExecutionTime: DataStart.AddMinutes(120),
                estimatedJobDuration: TimeSpan.FromMinutes(10),
                expectedBestExecutionTime: DataStart.AddMinutes(21),
                expectedCarbonIntensity: 19.5
            ),
            TestRowNoForecast(
                "Execution starts after data interval",
                earliestExecutionTime: DataStart.AddMinutes(33),
                latestExecutionTime: DataStart.AddMinutes(120),
                estimatedJobDuration: TimeSpan.FromMinutes(10)
            ),
            TestRow(
                "Job shorter than data resolution",
                earliestExecutionTime: DataStart,
                latestExecutionTime: DataStart.AddMinutes(120),
                estimatedJobDuration: TimeSpan.FromSeconds(5),
                expectedBestExecutionTime: DataStart.AddMinutes(20),
                expectedCarbonIntensity: 5
            ),
            TestRowNoForecast(
                "Job longer than data interval",
                earliestExecutionTime: DataStart,
                latestExecutionTime: DataStart.AddHours(5),
                estimatedJobDuration: TimeSpan.FromHours(1)
            ),
            TestRow(
                "18 minute job",
                earliestExecutionTime: DataStart,
                latestExecutionTime: DataStart.AddHours(2),
                estimatedJobDuration: TimeSpan.FromMinutes(18), 
                expectedBestExecutionTime: DataStart.AddMinutes(0), 
                expectedCarbonIntensity: 25
            )
        ];

        static object[] TestRow(
            string testCase,
            DateTimeOffset earliestExecutionTime, 
            DateTimeOffset latestExecutionTime, 
            TimeSpan estimatedJobDuration,
            DateTimeOffset expectedBestExecutionTime,
            double expectedCarbonIntensity) =>
        [
            testCase,
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

        static object[] TestRowNoForecast(
            string testCase,
            DateTimeOffset earliestExecutionTime, 
            DateTimeOffset latestExecutionTime, 
            TimeSpan estimatedJobDuration) =>
        [
            testCase,
            earliestExecutionTime,
            latestExecutionTime,
            estimatedJobDuration,
            ExecutionTime.NoForecast
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