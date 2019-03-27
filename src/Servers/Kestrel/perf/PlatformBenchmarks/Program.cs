// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PlatformBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(BenchmarkApplication.ApplicationName);
            Console.WriteLine(BenchmarkApplication.Paths.Plaintext);
            Console.WriteLine(BenchmarkApplication.Paths.Json);
            DateHeader.SyncDateTimer();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseBenchmarksConfiguration(config);
                    webBuilder.UseKestrel((context, options) =>
                    {
                        IPEndPoint endPoint = context.Configuration.CreateIPEndPoint();

                        options.Listen(endPoint, builder =>
                        {
                            builder.UseHttpApplication<BenchmarkApplication>();
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });

            return hostBuilder;
        }
    }
}
