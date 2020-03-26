using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BlazorNativeJs;
using Microsoft.JSInterop;

namespace BlazorApp.Client
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("app");
			NativeJs.Initialize((JSInProcessRuntime)builder.Services.BuildServiceProvider().GetRequiredService<IJSRuntime>());

			await builder.Build().RunAsync();
		}
	}
}
