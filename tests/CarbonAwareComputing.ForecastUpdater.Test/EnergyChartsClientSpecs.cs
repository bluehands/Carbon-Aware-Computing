using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Unicode;
using CarbonAware.Model;
using FunicularSwitch;
// ReSharper disable InconsistentNaming


namespace CarbonAwareComputing.ForecastUpdater.Test
{
    public abstract class EnergyChartsTransformContextSpecification : ContextSpecification
    {
        private EnergyChartsClient? m_EnergyChartsClient;
        protected string? m_ContentFile;
        protected string? m_Country;
        protected Task<Result<EmissionsForecast>>? m_Forecast;

        protected override void Given()
        {
            m_EnergyChartsClient = new EnergyChartsClient(GetContent);
            base.Given();
        }

        protected override void When()
        {
            var root = m_EnergyChartsClient!.GetForecastAsync(m_Country!);
            m_Forecast = root.Bind(
                f => EnergyChartsTransform.ImportForecast(f, m_Country!)
            );

        }

        private Task<Result<string>> GetContent(Uri arg)
        {
            if (string.IsNullOrWhiteSpace(m_ContentFile))
            {
                return Task.FromResult(Result.Error<string>("No Content"));
            }
            return Task.FromResult(Result.Ok(m_ContentFile!));
        }
    }


    [TestClass]
    public class Given_a_energy_chart_client_with_at_data : EnergyChartsTransformContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.at);
            m_Country = "at";
            base.Given();
        }

        [TestMethod]
        public void Then_a_the_forecast_is_generated()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.Any());
        }
        [TestMethod]
        public void Then_all_forecast_ratings_are_set()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
        }
        [TestMethod]
        public void Then_all_forecast_duration_is_15_minute()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(15)));
        }

    }

    [TestClass]
    public class Given_a_energy_chart_client_with_fr_data : EnergyChartsTransformContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.fr);
            m_Country = "fr";
            base.Given();
        }

        [TestMethod]
        public void Then_a_the_forecast_is_generated()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.Any());
        }
        [TestMethod]
        public void Then_all_forecast_ratings_are_set()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
        }
        [TestMethod]
        public void Then_all_forecast_duration_is_15_minute()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(60)));
        }

    }
    [TestClass]
    public class Given_a_energy_chart_client_with_ch_data : EnergyChartsTransformContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.ch);
            m_Country = "ch";
            base.Given();
        }

        [TestMethod]
        public void Then_a_the_forecast_is_generated()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.Any());
        }
        [TestMethod]
        public void Then_all_forecast_ratings_are_set()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
        }
        [TestMethod]
        public void Then_all_forecast_duration_is_60_minute()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(60)));
        }
    }
    [TestClass]
    public class Given_a_energy_chart_client_with_de_and_no_forecast_data : EnergyChartsTransformContextSpecification
    {
        protected override void Given()
        {
            m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.de_no_forecast);
            m_Country = "de";
            base.Given();
        }

        [TestMethod]
        public void Then_a_the_forecast_is_generated()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.Any());
        }
        [TestMethod]
        public void Then_all_forecast_ratings_are_set()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
        }
        [TestMethod]
        public void Then_all_forecast_duration_is_15_minute()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(15)));
        }
        [TestMethod]
        public void Then_no_forecast_is_given()
        {
            var forecast = m_Forecast.Result.GetValueOrDefault();
            Assert.IsNotNull(forecast);
            Assert.IsTrue(forecast.ForecastData.All(f => f.Time <DateTimeOffset.Parse("2023-04-12T00:00:00+00")));
        }

    }
}
