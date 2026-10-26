#region license
// This file is part of Vocaluxe.
//
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using VocaluxeWeb.Api;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Hosting
{
    static class CStartup
    {
        private const string _IndexFile = "index.html";

        public static void ConfigureServices(IServiceCollection services, CWebHost host, IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CWebSettingsStore settings,
            CWebServerOptions options)
        {
            services.AddSingleton(host);
            services.AddSingleton(remote);
            services.AddSingleton(dispatcher);
            services.AddSingleton(settings);
            services.AddSingleton<CSessionStore>();
            services.AddSingleton<CSongLibrary>();
            services.AddSingleton<CLiveEvents>();
            services.AddSingleton<CQueueService>();
            services.AddSingleton(provider => new CLiveMonitor(remote, provider.GetRequiredService<CLiveEvents>(), provider.GetRequiredService<CQueueService>(),
                provider.GetRequiredService<CSongLibrary>(), options.OnError));
            services.AddSingleton(new CImageService(options.CachePath));

            services.AddResponseCompression(compression =>
            {
                compression.EnableForHttps = true;
                compression.Providers.Add<GzipCompressionProvider>();
            });

            // The entry assembly is Vocaluxe.exe, so MVC would not find our controllers on its own
            services.AddMvcCore(mvc => mvc.Filters.Add(new CExceptionFilter(options.OnError)))
                .AddApplicationPart(typeof(CStartup).Assembly)
                .AddApiExplorer()
                .AddDataAnnotations()
                .AddJsonFormatters(json => json.Converters.Add(new StringEnumConverter()))
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Vocaluxe API", Version = "v1" });
                foreach (var folder in new[] { AppDomain.CurrentDomain.BaseDirectory, Path.GetDirectoryName(typeof(CStartup).Assembly.Location) })
                {
                    var xmlDoc = Path.Combine(folder, "VocaluxeWeb.xml");
                    if (File.Exists(xmlDoc))
                    {
                        swagger.IncludeXmlComments(xmlDoc);
                        break;
                    }
                }
            });
            services.AddSwaggerGenNewtonsoftSupport();
        }

        public static void Configure(IApplicationBuilder app, CWebServerOptions options)
        {
            app.UseResponseCompression();

            app.UseSwagger();
            app.UseSwaggerUI(ui => ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Vocaluxe API v1"));

            app.UseMvc();

            if (!string.IsNullOrEmpty(options.WebRoot) && Directory.Exists(options.WebRoot))
            {
                var files = new PhysicalFileProvider(Path.GetFullPath(options.WebRoot));
                var contentTypes = new FileExtensionContentTypeProvider();
                contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = files,
                    ContentTypeProvider = contentTypes,
                    OnPrepareResponse = ctx => _SetCacheHeaders(ctx.Context.Response, ctx.File.Name)
                });

                app.Run(context => _ServeIndex(context, files));
            }
            else
            {
                app.Run(context => _NotFound(context));
            }
        }

        /// Vite puts a hash into every asset name, so those can be cached forever. index.html must always be revalidated.
        private static void _SetCacheHeaders(HttpResponse response, string fileName)
        {
            response.Headers["Cache-Control"] = fileName == _IndexFile ? "no-cache" : "public, max-age=31536000, immutable";
        }

        /// Single page app fallback: every unknown path outside the API gets index.html, the WebApp's router handles the rest.
        private static Task _ServeIndex(HttpContext context, IFileProvider files)
        {
            var index = files.GetFileInfo(_IndexFile);
            if (!index.Exists || _IsApiPath(context.Request.Path) || context.Request.Method != HttpMethods.Get)
            {
                return _NotFound(context);
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            _SetCacheHeaders(context.Response, _IndexFile);
            return context.Response.SendFileAsync(index, context.RequestAborted);
        }

        private static bool _IsApiPath(PathString path)
        {
            return path.StartsWithSegments("/api") || path.StartsWithSegments("/swagger");
        }

        private static Task _NotFound(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }
    }
}
