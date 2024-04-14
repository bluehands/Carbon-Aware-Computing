using System.Text;
using CarbonAware.Model;

namespace CarbonAwareComputing.ExecutionForecast.Test
{
    public abstract class ExecutionForecastContextSpecification : ContextSpecification
    {
        private CarbonAwareDataProviderWithCustomForecast m_Provider;
        protected string m_ContentFile;
        protected ComputingLocation m_Location;
        protected DateTimeOffset m_Now;
        protected GridCarbonIntensity m_Intensity;

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
            Assert.IsTrue(m_Intensity is EmissionData { Value: 151.2 });
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
            Assert.IsTrue(m_Intensity is NoData);
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
            Assert.IsTrue(m_Intensity is EmissionData { Value: 185.3 });
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
            Assert.IsTrue(m_Intensity is EmissionData { Value: 0.0 });
        }
    }
}
