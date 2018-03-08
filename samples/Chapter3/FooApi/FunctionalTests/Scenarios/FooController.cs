using FluentAssertions;
using FooApi.Controllers;
using FooApi.Models;
using FunctionalTests.Seedwork;
using Microsoft.AspNetCore.TestHost;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FunctionalTests.Scenarios
{
    [Collection("Foo")]
    public class foo_api_should
    {
        private readonly FooFixture Given;

        public foo_api_should(FooFixture fooFixture)
        {
            Given = fooFixture;
        }

        [Fact]
        public async Task get_bar_when_requested()
        {
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo=>foo.Get(1),new { version = 1 })
                .WithIdentity(new List<Claim>() {  new Claim("Permission","Read")})
                .GetAsync();

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task get_forbideen_if_not_authenticated_when_requested()
        {
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Get(1), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "NonReadClaim") })
                .GetAsync();

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task post_new_bar()
        {
            var bar = new Bar() { Id = 1 };
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Post(bar), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "Write") })
                .PostAsync();

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task post_get_forbidden_if_not_authenticated_when_requested()
        {
            var bar = new Bar() { Id = 1 };
            var response = await Given.FooServer
                .CreateHttpApiRequest<FooController>(foo => foo.Post(bar), new { version = 1 })
                .WithIdentity(new List<Claim>() { new Claim("Permission", "NonWriteClaim") })
                .PostAsync();

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Forbidden);
        }
    }


    static class FooAPI
    {
        static string BASEURI = "api/v1/foo";

        public static class Get
        {
            public static string Bar(int id)
            {
                return $"{BASEURI }?id={id}";
            }
        }

        public static class Post
        {
            public static string Bar()
            {
                return BASEURI;
            }
        }
    }
    
}
