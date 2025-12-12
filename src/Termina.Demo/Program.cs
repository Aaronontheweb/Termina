using Microsoft.Extensions.Hosting;
using Termina.Demo.Pages;
using Termina.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register Termina with reactive pages
builder.Services.AddTermina("counter", termina =>
{
    termina.RegisterPage<CounterPage, CounterViewModel>("counter");
    termina.RegisterPage<TodoListPage, TodoListViewModel>("todo-list");
});

var host = builder.Build();
await host.RunAsync();
