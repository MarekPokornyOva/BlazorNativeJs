#region using
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
#endregion using

namespace BlazorNativeJs.Server
{
	public static class NativeJsApplicationBuilderExtensions
	{
		public static IApplicationBuilder UseNativeJsHack(this IApplicationBuilder app)
		{
			app.Use((httpContext,next) =>
				httpContext.Request.Path.ToString() switch
				{
					"/_framework/blazor.webassembly.js" => InjectNativeJs(httpContext,next),
					"/NativeJs.js" => WriteNativeJs(httpContext),
					_ => next()
				}
			);
			return app;
		}

		static async Task InjectNativeJs(HttpContext httpContext,Func<Task> fileProvider)
		{
			HttpResponse res = httpContext.Response;
			Stream realRes = res.Body;
			MemoryStream ms;
			res.Body=ms=new MemoryStream();
			await fileProvider();
			ms.Position=0;
			if (res.StatusCode==200)
			{
				string s = new StreamReader(ms).ReadToEnd();

				int headStart = s.IndexOf("fromDOMEvent=")+13;
				int headEnd = s.IndexOf('{',headStart)+1;
				int funcEnd = headEnd;
				int openings = 1;
				while (openings!=0)
				{
					funcEnd++;
					char chr = s[funcEnd];
					if (chr=='{')
						openings++;
					else if (chr=='}')
						openings--;
				}

				res.ContentLength+=34+(headEnd-headStart)+6;
				StreamWriter sw = new StreamWriter(realRes);
				await sw.WriteAsync(s[0..headEnd]);
				await sw.WriteAsync("return NativeJs.handleEventArgs(t,");
				await sw.WriteAsync(s[headStart..headEnd]);
				await sw.WriteAsync(s[headEnd..funcEnd]);
				await sw.WriteAsync("}(t));");
				await sw.WriteAsync(s[funcEnd..]);
				await sw.FlushAsync();
			}
			else
				await ms.CopyToAsync(realRes);
		}

		static async Task WriteNativeJs(HttpContext httpContext)
		{
#warning TODO: handle ETAG => 304
			HttpResponse response = httpContext.Response;
			response.ContentType="text/javascript";
			using (Stream src = typeof(NativeJsApplicationBuilderExtensions).Assembly.GetManifestResourceStream($"{nameof(BlazorNativeJs)}.{nameof(BlazorNativeJs.Server)}.NativeJs.js"))
				await src.CopyToAsync(response.Body);
		}
	}
}
