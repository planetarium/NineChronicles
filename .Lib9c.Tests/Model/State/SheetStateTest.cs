namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Libplanet;
    using Nekoyume;
    using Nekoyume.TableData;
    using Xunit;
    using Xunit.Abstractions;

    public class SheetStateTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SheetStateTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void PrintSheetAddresses()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ISheet));
            Assert.NotNull(assembly);

            IEnumerable<string> sheetNames = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ISheet).IsAssignableFrom(type))
                .Select(type => type.Name);
            foreach (string sheetName in sheetNames)
            {
                Address address = Addresses.GetSheetAddress(sheetName);
                _testOutputHelper.WriteLine("{0}: {1}", sheetName, address.ToHex());
            }
        }
    }
}
