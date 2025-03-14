namespace CarbonAwareComputing.ExecutionForecast.Test
{
    [TestClass]
    public abstract class ContextSpecification
    {
        public static TestContext TestContext { get; set; } = null!;

        [AssemblyInitialize]
        public static void SetupTests(TestContext testContext)
        {
            TestContext = testContext;
        }

        [TestInitialize]
        public void EstablishContext()
        {
            Given();
            When();
        }

        [TestCleanup]
        public void CleanUpContext()
        {
            CleanUp();
        }

        protected virtual void CleanUp()
        {
        }

        protected virtual void Given()
        {
            
        }

        protected abstract void When();
    }
}