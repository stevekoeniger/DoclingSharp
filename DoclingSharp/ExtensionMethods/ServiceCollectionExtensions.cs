using DoclingSharp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DoclingSharp.ExtensionMethods
{
    /// <summary>
    /// The service collection extension methods for add docling.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add docling to the service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration for the service collection.</param>
        /// <returns></returns>
        public static IServiceCollection AddDocling(this IServiceCollection services, Action<DoclingOptions> configure)
        {
            services.Configure(configure);

            services.AddHttpClient("Docling");

            services.AddSingleton<DoclingClient>();
            services.AddSingleton<DocumentChunker>();

            return services;
        }
    }
}
