// ReSharper disable InconsistentNaming
using CarbonAware.Model;
using CarbonAwareComputing.ForecastUpdater.UKNationalGrid;
using FunicularSwitch;
using System.Text;

namespace CarbonAwareComputing.ForecastUpdater.Test;

public abstract class UKNationalGridTransformContextSpecification : ContextSpecification
{
    private UKNationalGridClient? m_UKNationalGridClient;
    protected string? m_ContentFile;
    protected Option<UKRegions> m_Region = Option<UKRegions>.None;
    protected Task<Result<EmissionsForecast>>? m_Forecast;

    protected override void Given()
    {
        m_UKNationalGridClient = new UKNationalGridClient(GetContent);
        base.Given();
    }

    protected override void When()
    {
        var root = m_UKNationalGridClient!.GetForecastAsync(m_Region);
        m_Forecast = root.Bind(
            f => UKNationalGridTransform.ImportForecast(f, m_Region)
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
public class Given_a_uk_national_grid_client_with_london_data : UKNationalGridTransformContextSpecification
{
    protected override void Given()
    {
        m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.london);
        m_Region = UKRegions.London;
        base.Given();
    }

    [TestMethod]
    public void Then_a_the_forecast_is_generated()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.Any());
    }
    [TestMethod]
    public void Then_all_forecast_ratings_are_set()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
    }
    [TestMethod]
    public void Then_all_forecast_duration_is_30_minute()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(30)));
    }

}
[TestClass]
public class Given_a_uk_national_grid_client_with_uk_data : UKNationalGridTransformContextSpecification
{
    protected override void Given()
    {
        m_ContentFile = Encoding.UTF8.GetString(Properties.Resources.uk);
        m_Region = Option<UKRegions>.None;
        base.Given();
    }

    [TestMethod]
    public void Then_a_the_forecast_is_generated()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.Any());
    }
    [TestMethod]
    public void Then_all_forecast_ratings_are_set()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.All(f => f.Rating > 0));
    }
    [TestMethod]
    public void Then_all_forecast_duration_is_30_minute()
    {
        var forecast = m_Forecast!.Result.GetValueOrDefault();
        Assert.IsNotNull(forecast);
        Assert.IsTrue(forecast.ForecastData.All(f => f.Duration == TimeSpan.FromMinutes(30)));
    }

}