#:package AWSSDK.S3@4.0.100
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@9.0.5
#:package Microsoft.Extensions.Configuration.Binder@9.0.5
#:package DotNetEnv@3.1.1
#:package Spectre.Console.Json@0.50.0

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var settings = LoadS3Settings();
using var s3Client = CreateS3Client(settings);
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

Console.WriteLine($"Отправка нового текстового файла через передачу текста в ContentBody.");
await TryExecute(
    s3Client.PutObjectAsync(new PutObjectRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-text.txt",
        ContentBody = "Привет, это файл созданный из текста!",
    }));

Console.WriteLine($"Отправка нового текстового файла.");
// Имитация чтения файла через MemoryStream вместо File.OpenRead
await using var file = new MemoryStream(Encoding.UTF8.GetBytes("Привет, это файл переданный напрямую!"));
await TryExecute(
    s3Client.PutObjectAsync(new PutObjectRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-file.txt",
        InputStream = file
    }));

Console.WriteLine($"Получение списка объектов в бакете {settings.BucketName}.");
await TryExecute(
    s3Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = settings.BucketName }));

Console.WriteLine($"Получение объекта example-from-text.txt.");
await TryRetrieve(
    s3Client.GetObjectAsync(new GetObjectRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-text.txt"
    }));

Console.WriteLine($"Получение метаданных объекта example-from-text.txt.");
await TryExecute(
    s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-text.txt"
    }));

Console.WriteLine($"Копирование объекта example-from-text.txt.");
await TryExecute(
    s3Client.CopyObjectAsync(new CopyObjectRequest
    {
        SourceBucket = settings.BucketName,
        SourceKey = "example-from-text.txt",
        DestinationBucket = settings.BucketName,
        DestinationKey = "example-from-text-copy.txt"
    }));

Console.WriteLine("Присвоение тега `deleted=true` файлу `example-from-text-copy.txt`");
await TryExecute(
    s3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
    {
        BucketName = settings.BucketName,
        Tagging = new Tagging { TagSet = [new Tag { Key = "deleted", Value = "true" }] },
        Key = "example-from-text-copy.txt"
    })
);

Console.WriteLine("Получение информации о тегах присвоенных файлу `example-from-text-copy.txt`");
await TryExecute(
    s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-text-copy.txt"
    })
);

Console.WriteLine("Создание правил жизненного цикла файлов: " +
    "правила по тегу `deleted=true`, по названию папок `deleted/`");
LifecycleRule[] rules =
[
    new()
    {
        Id = "DeletedTagRetention",
        Status = LifecycleRuleStatus.Enabled,
        Filter = new LifecycleFilter
        {
            LifecycleFilterPredicate = new LifecycleTagPredicate
            {
                Tag = new Tag { Key = "deleted", Value = "true" }
            }
        },
        Expiration = new LifecycleRuleExpiration { Days = 1 }
    },
    new()
    {
        Id = "DeletedFolderRetention",
        Status = LifecycleRuleStatus.Enabled,
        Filter = new LifecycleFilter
        {
            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = "deleted/" }
        },
        Expiration = new LifecycleRuleExpiration { Days = 1 }
    }
];
await TryExecute(
    s3Client.PutLifecycleConfigurationAsync(new PutLifecycleConfigurationRequest
    {
        BucketName = settings.BucketName,
        Configuration = new LifecycleConfiguration
        {
            Rules = [.. rules]
        }
    })
);

Console.WriteLine("Получение правил жизненного цикла файлов");
await TryExecute(
    s3Client.GetLifecycleConfigurationAsync(new GetLifecycleConfigurationRequest
    {
        BucketName = settings.BucketName
    })
);

Console.WriteLine("Удаление всех тегов присвоенных файлу `example-from-text-copy.txt`");
await TryExecute(
    s3Client.DeleteObjectTaggingAsync(new DeleteObjectTaggingRequest
    {
        BucketName = settings.BucketName,
        Key = "example-from-text-copy.txt"
    })
);

Console.WriteLine("Удаление всех правил жизненного цикла файлов");
await TryExecute(
    s3Client.DeleteLifecycleConfigurationAsync(new DeleteLifecycleConfigurationRequest
    {
        BucketName = settings.BucketName
    })
);

Console.WriteLine($"Удаление всех объектов.");
await TryExecute(
    s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
    {
        BucketName = settings.BucketName,
        Objects = new List<KeyVersion>
        {
            new KeyVersion { Key = "example-from-text.txt" },
            new KeyVersion { Key = "example-from-file.txt" },
            new KeyVersion { Key = "example-from-text-copy.txt" }
        }
    }));

Console.WriteLine($"Удаление бакета {settings.BucketName}.");
await TryExecute(
    s3Client.DeleteBucketAsync(new DeleteBucketRequest { BucketName = settings.BucketName }));

static AmazonS3Client CreateS3Client(S3Settings s3Settings)
{
    var config = new AmazonS3Config
    {
        ServiceURL = s3Settings.ServiceUrl,
        ForcePathStyle = true,
		AuthenticationRegion = s3Settings.Region
    };
    var credentials = new BasicAWSCredentials(s3Settings.AccessKey, s3Settings.SecretKey);
    var client = new AmazonS3Client(credentials, config);
    return client;
}

static S3Settings LoadS3Settings()
{
    Env.Load();

    var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    var settings = configuration.GetSection("S3").Get<S3Settings>();

    if (settings == null)
        throw new ArgumentNullException(nameof(settings), "Настройки S3 не могут быть пустыми.");
    if (!settings.IsValid())
        throw new ArgumentException("Настройки S3 заполнены не полностью. Проверьте файл .env и переменные окружения.");
    return settings;
}

#region Рендеринг и обработка ошибок
static async Task TryExecute<T>(Task<T> action) where T : AmazonWebServiceResponse
{
    try
    {
        var response = await action;
        var json = SerializeAwsResponse(response);
        Console.WriteLine($"Операция выполнена успешно. Результат: \n");
        // JSON стайлинг в формате Visual Studio Code
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
            .Expand()
            .Padding(2, 1)
            .RoundedBorder()
            .BorderColor(Color.Yellow));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}\n{ex.StackTrace}\nВложенная ошибка: {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
    }
}
static async Task TryRetrieve<T>(Task<T> action) where T : StreamResponse
{
    try
    {
        using var response = await action;
        using var reader = new StreamReader(response.ResponseStream);
        // Исключительно для текстовых файлов
        var text = await reader.ReadToEndAsync();
        Console.WriteLine("Загрузка выполнена успешно. Содержимое объекта: \n");
        AnsiConsole.Write(
            new Panel(new Padder(new Text(text)).PadBottom(1).PadTop(1))
            .Header("Объект S3")
            .Expand()
            .RoundedBorder()
            .BorderColor(Color.Yellow));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}\n{ex.StackTrace}\nВложенная ошибка: {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
    }
}

static string SerializeAwsResponse(AmazonWebServiceResponse response)
{
    return response switch
    {
        PutBucketResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.PutBucketResponse),
        GetBucketLocationResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.GetBucketLocationResponse),
        ListBucketsResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.ListBucketsResponse),
        PutObjectResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.PutObjectResponse),
        ListObjectsV2Response v => JsonSerializer.Serialize(v, AppJsonContext.Default.ListObjectsV2Response),
        GetObjectMetadataResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.GetObjectMetadataResponse),
        CopyObjectResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.CopyObjectResponse),
        PutObjectTaggingResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.PutObjectTaggingResponse),
        GetObjectTaggingResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.GetObjectTaggingResponse),
        PutLifecycleConfigurationResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.PutLifecycleConfigurationResponse),
        GetLifecycleConfigurationResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.GetLifecycleConfigurationResponse),
        DeleteObjectTaggingResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.DeleteObjectTaggingResponse),
        DeleteLifecycleConfigurationResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.DeleteLifecycleConfigurationResponse),
        DeleteObjectsResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.DeleteObjectsResponse),
        DeleteBucketResponse v => JsonSerializer.Serialize(v, AppJsonContext.Default.DeleteBucketResponse),
        _ => JsonSerializer.Serialize(
            new FallbackResponse(response.GetType().Name, (int)response.HttpStatusCode, response.ResponseMetadata?.RequestId),
            AppJsonContext.Default.FallbackResponse)
    };
}
#endregion

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
record S3Settings(string ServiceUrl, string AccessKey, string SecretKey, string BucketName, string Region)
{
    public bool IsValid()
        => !string.IsNullOrEmpty(AccessKey) &&
        !string.IsNullOrEmpty(SecretKey) &&
        !string.IsNullOrEmpty(ServiceUrl) &&
        !string.IsNullOrEmpty(BucketName) &&
        !string.IsNullOrEmpty(Region);
}

//AOT friendly serialization for AWS SDK responses
record FallbackResponse(string ResponseType, int HttpStatusCode, string? RequestId);

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(PutBucketResponse))]
[JsonSerializable(typeof(GetBucketLocationResponse))]
[JsonSerializable(typeof(ListBucketsResponse))]
[JsonSerializable(typeof(PutObjectResponse))]
[JsonSerializable(typeof(ListObjectsV2Response))]
[JsonSerializable(typeof(GetObjectMetadataResponse))]
[JsonSerializable(typeof(CopyObjectResponse))]
[JsonSerializable(typeof(PutObjectTaggingResponse))]
[JsonSerializable(typeof(GetObjectTaggingResponse))]
[JsonSerializable(typeof(PutLifecycleConfigurationResponse))]
[JsonSerializable(typeof(GetLifecycleConfigurationResponse))]
[JsonSerializable(typeof(DeleteObjectTaggingResponse))]
[JsonSerializable(typeof(DeleteLifecycleConfigurationResponse))]
[JsonSerializable(typeof(DeleteObjectsResponse))]
[JsonSerializable(typeof(DeleteBucketResponse))]
[JsonSerializable(typeof(FallbackResponse))]
internal partial class AppJsonContext : JsonSerializerContext
{
}