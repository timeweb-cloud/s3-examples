#:package AWSSDK.S3@3.5.10.2
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@9.0.5
#:package Microsoft.Extensions.Configuration.Binder@9.0.5
#:package DotNetEnv@3.1.1
#:package Spectre.Console.Cli@0.50.0
#:package Spectre.Console.Json@0.50.0

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Json;
using System;
using System.Text.Json;

Env.Load();
var builder = new ConfigurationBuilder()
    .AddEnvironmentVariables();
var configuration = builder.Build();
var settings = configuration.GetSection("S3").Get<S3Settings>()!;

var s3Client = CreateS3Client(settings);
Console.WriteLine("S3 Клиент успешно создан и готов к работе.");

Console.WriteLine($"Создание бакета {settings.BucketName}.");
await TryExecute(
    s3Client.PutBucketAsync(new PutBucketRequest { BucketName = settings.BucketName }));
    
Console.WriteLine($"Получение информации о регионе бакета {settings.BucketName}.");
await TryExecute(
    s3Client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = settings.BucketName }));

Console.WriteLine($"Получение списка бакетов.");
await TryExecute(
    s3Client.ListBucketsAsync());


static AmazonS3Client CreateS3Client(S3Settings? s3Settings)
{
    if (s3Settings == null)
        throw new ArgumentNullException(nameof(s3Settings), "Настройки S3 не могут быть пустыми.");
    if (string.IsNullOrEmpty(s3Settings.AccessKey) || string.IsNullOrEmpty(s3Settings.SecretKey))
        throw new ArgumentException("Все поля в настройках S3 должны быть заполнены, включая AccessKey, SecretKey, BucketName и ServiceUrl.");

    var config = new AmazonS3Config
    {
        ServiceURL = s3Settings.ServiceUrl,
        ForcePathStyle = true
    };
    var credentials = new BasicAWSCredentials(s3Settings.AccessKey, s3Settings.SecretKey);
    var client = new AmazonS3Client(credentials, config);
    return client;
}

#region 
static async Task TryExecute<T>(Task<T> action) where T : AmazonWebServiceResponse
{
    try
    {
        var response = await action;
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Console.WriteLine($"Операция выполнена успешно. Результат: \n");
        // JSON в формате Visual Studio Code
        AnsiConsole.Write(
            new Panel(
                new JsonText(json)
					.BracesColor(Color.Grey84)
					.BracketColor(Color.Grey84)
					.ColonColor(Color.Grey84)
					.CommaColor(Color.Grey84)
					.StringColor(Color.LightSalmon3_1)
					.NumberColor(Color.DarkSeaGreen3_1)
					.BooleanColor(Color.SkyBlue3)
					.NullColor(Color.SkyBlue3)
					.MemberColor(Color.SteelBlue1_1)
            )
            .Header("JSON")
            .Collapse()
            .RoundedBorder()
            .BorderColor(Color.Yellow));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

/// <summary>
/// Класс для хранения настроек подключения к Amazon S3.
/// Используется для конфигурации клиента S3.
/// 
/// Заполняется автоматически из переменных окружения (файл .env)
/// 
/// Данные для подключения к S3 хранилищу Timeweb Cloud 
/// можно найти во вкладке `Дашборд` в разделе `Хранилище S3` 
/// в личном кабинете Timeweb Cloud.
/// </summary>
class S3Settings
{
    public required string ServiceUrl { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
    public required string Region { get; set; }

    public bool IsValid()
        => !string.IsNullOrEmpty(AccessKey) &&
        !string.IsNullOrEmpty(SecretKey) &&
        !string.IsNullOrEmpty(ServiceUrl) &&
        !string.IsNullOrEmpty(BucketName) &&
        !string.IsNullOrEmpty(Region);
}
#endregion