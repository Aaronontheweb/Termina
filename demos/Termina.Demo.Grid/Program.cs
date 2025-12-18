// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Termina.Demo.Grid;
using Termina.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register Termina with the GridNode demo page
builder.Services.AddTermina("/dashboard", termina =>
{
    termina.RegisterRoute<DashboardPage, DashboardViewModel>("/dashboard");
});

var host = builder.Build();
await host.RunAsync();
