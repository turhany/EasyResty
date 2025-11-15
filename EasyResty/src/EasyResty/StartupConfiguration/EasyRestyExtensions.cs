using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Resty.Abstract;
using Resty.Concrete;

namespace Resty.StartupConfiguration
{
    public static class EasyRestyExtensions
    {
        public static WebApplicationBuilder WithEasyResty(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IRestClient, RestClient>();
            builder.Services.AddScoped<IEasyRestyClient, RestSharpEasyRestyClient>();

            return builder;
        }
    }
}
