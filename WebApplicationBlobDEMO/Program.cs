using Azure.Storage.Blobs;

namespace WebApplicationBlobDEMO
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            string connectionString = builder.Configuration.GetConnectionString("AzureStorage");

            builder.Services.AddSingleton(x => new BlobServiceClient(connectionString));

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
