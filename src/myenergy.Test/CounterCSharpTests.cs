using Microsoft.JSInterop;
using Moq;
using myenergy.Pages;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;

namespace myenergy.Test
{
    /// <summary>
    /// These tests are written entirely in C#.
    /// Learn more at https://bunit.dev/docs/getting-started/writing-tests.html#creating-basic-tests-in-cs-files
    /// </summary>
    public class CounterCSharpTests : TestContext
    {
        [Fact]
        public void CounterStartsAtZero()
        {
            // Arrange
            var cut = RenderComponent<Counter>();

            // Assert that content of the paragraph shows counter at zero
            cut.Find("p").MarkupMatches("<p>Current count: 0</p>");
        }

        [Fact]
        public void ClickingButtonIncrementsCounter()
        {
            // Arrange
            var cut = RenderComponent<Counter>();

            // Act - click button to increment counter
            cut.Find("button").Click();

            // Assert that the counter was incremented
            cut.Find("p").MarkupMatches("<p>Current count: 1</p>");
        }

        [Fact]
        public void Test()
        {
            var ctx = new TestContext();

            // read json file
            var data = ReadJsonFile("Data/data.json");

            // Mock HttpClient
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost/Data/data.json")
                    .Respond("application/json", data); // Replace with your JSON content

            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
            ctx.Services.AddScoped(_ => client);



            ctx.Services.AddBlazorBootstrap();

            //window.blazorBootstrap.tabs.initialize

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            // Arrange
            var cut = ctx.RenderComponent<Home>();

            cut.WaitForState(() =>
            {
                var tab = cut.Find("button:contains('Day')");
                Assert.True(tab.GetAttribute("aria-selected") == "true");
                return true;
            });
        }

        public string ReadJsonFile(string filePath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            string json = File.ReadAllText(fullPath);
            return json;
        }
    }
}
