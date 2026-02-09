using Dapper;
using SyncDemo.Api.Data;
using SyncDemo.Api.Hubs;
using SyncDemo.Api.Services;
using SyncDemo.Api.Infrastructure.SignalR;
using SyncDemo.Api.Infrastructure.RabbitMQ;

// Register custom Dapper type handler for Oracle GUID strings
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Oracle connection factory
var oracleConnectionString = builder.Configuration.GetConnectionString("OracleConnection") 
    ?? "Data Source=localhost:1521/XEPDB1;User Id=syncuser;Password=syncpass;";
builder.Services.AddSingleton<IDbConnectionFactory>(new OracleConnectionFactory(oracleConnectionString));

// Register repositories
builder.Services.AddScoped<ISyncItemRepository, SyncItemRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDevicePermissionRepository, DevicePermissionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register services
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Register RabbitMQ service (for existing SyncItems functionality)
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
var rabbitMqUser = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
builder.Services.AddSingleton<IMessageQueueService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
    return new RabbitMqService(rabbitMqHost, rabbitMqPort, rabbitMqUser, rabbitMqPassword, logger);
});

// Register infrastructure services for Oracle AQ event-driven architecture
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<MessagePublisher>>();
    return new MessagePublisher(rabbitMqHost, rabbitMqPort, rabbitMqUser, rabbitMqPassword, logger);
});

// Register Oracle AQ background service (Event-Driven Architecture)
builder.Services.AddHostedService<OracleQueueService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SyncHub>("/synchub");

app.Run();
