using AttendanceTrackerServer.Controllers;
using Newtonsoft.Json;
using System.Net;

namespace AttendanceTrackerServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST computer
            Console.WriteLine(hostName);

            // Get the IPs of the computer, and grab the last one, which seems to be the correct IPv4 for the computer
            IPAddress[] myIPs = Dns.GetHostEntry(hostName).AddressList;
            IPAddress myIP = myIPs[4];
            Console.WriteLine($"My IP Address is : {myIP}");

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            new Thread(() => AttendeeController.FileAccess()).Start();

            app.Run($"http://{myIP}:6969");
        }
    }
}
