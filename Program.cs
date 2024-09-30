using DocuwareManagementWebAPI.Services;
using Microsoft.Extensions.Logging;

namespace DocuwareManagementWebAPI
{
    /// <summary>
    /// The entry point for the DocuWare Management Web API application.
    /// Configures services, middleware, and the HTTP request pipeline.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point of the application.
        /// Initializes the web application, configures services, sets up middleware, and adds logging support.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders(); // Optional: Clear default providers if not needed.
            builder.Logging.AddConsole();     // Logs to the console.
            builder.Logging.AddDebug();       // Logs to the debug output.
            builder.Logging.AddEventSourceLogger(); // Logs to Event Source (Windows Event Log).

            // Add services to the container.
            builder.Services.AddControllers();

            // Add CORS policy to allow requests from the Angular frontend.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            // Register the DocuWare service as a scoped service in the DI container.
            // This binds the interface IDocuWareService to the concrete DocuWareService class.
            builder.Services.AddScoped<IDocuWareService, DocuWareService>();

            // Add Swagger/OpenAPI support for API documentation.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the middleware pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Enable the CORS policy for Angular application access.
            app.UseCors("AllowAngularApp");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
