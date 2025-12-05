using Mass.Core.Services;
using Mass.Spec.Contracts.Ipc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mass.Core.Tests.Integration;

public class IpcIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIpcService _ipcService;

    public IpcIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIpcService, IpcService>();
        _serviceProvider = services.BuildServiceProvider();
        _ipcService = _serviceProvider.GetRequiredService<IIpcService>();
    }

    [Fact]
    public async Task SendRequest_WithValidMessage_ReturnsTypedResponse()
    {
        // Arrange
        bool handlerCalled = false;
        _ipcService.RegisterHandler("TestRequest", async req =>
        {
            handlerCalled = true;
            return new IpcResponse
            {
                Success = true,
                CorrelationId = req.CorrelationId,
                Data = new Dictionary<string, object> { ["Result"] = "OK" }
            };
        });

        await _ipcService.StartServerAsync(_serviceProvider);

        var request = new IpcRequest
        {
            RequestType = "TestRequest",
            CorrelationId = Guid.NewGuid().ToString(),
            Data = new Dictionary<string, object> { ["Test"] = "Value" }
        };

        // Act
        var response = await _ipcService.SendRequestAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(request.CorrelationId, response.CorrelationId);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task RegisterHandler_HandlesIncomingMessage()
    {
        // Arrange
        IpcRequest? receivedRequest = null;
        _ipcService.RegisterHandler("DataSync", async req =>
        {
            receivedRequest = req;
            return new IpcResponse
            {
                Success = true,
                CorrelationId = req.CorrelationId
            };
        });

        await _ipcService.StartServerAsync(_serviceProvider);

        var request = new IpcRequest
        {
            RequestType = "DataSync",
            Data = new Dictionary<string, object> { ["JobId"] = "123" }
        };

        // Act
        await _ipcService.SendRequestAsync(request);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.Equal("DataSync", receivedRequest.RequestType);
        // System.Text.Json deserializes as JsonElement, so we need to convert
        var jobIdValue = receivedRequest.Data?["JobId"];
        var jobIdString = jobIdValue is System.Text.Json.JsonElement je ? je.GetString() : jobIdValue?.ToString();
        Assert.Equal("123", jobIdString);
    }

    [Fact]
    public async Task SendRequest_WithLargePayload_RejectsMessage()
    {
        // Arrange
        await _ipcService.StartServerAsync(_serviceProvider);

        var largeData = new Dictionary<string, object>();
        for (int i = 0; i < 100000; i++)
        {
            largeData[$"Key{i}"] = $"This is a very long string value to make the payload large {i}";
        }

        var request = new IpcRequest
        {
            RequestType = "LargeRequest",
            Data = largeData
        };

        // Act
        var response = await _ipcService.SendRequestAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("exceeds maximum size", response.ErrorMessage);
    }

    [Fact]
    public async Task MessageSerialization_UsesSystemTextJson()
    {
        // Arrange
        var testData = new Dictionary<string, object>
        {
            ["StringValue"] = "test",
            ["IntValue"] = 42,
            ["DateValue"] = DateTime.UtcNow,
            ["BoolValue"] = true
        };

        _ipcService.RegisterHandler("SerializationTest", async req =>
        {
            // Verify data was deserialized correctly (need to handle JsonElement)
            var stringVal = req.Data?["StringValue"] is System.Text.Json.JsonElement se ? se.GetString() : req.Data?["StringValue"]?.ToString();
            var intVal = req.Data?["IntValue"] is System.Text.Json.JsonElement ie ? ie.GetInt32() : Convert.ToInt32(req.Data?["IntValue"]);
            var boolVal = req.Data?["BoolValue"] is System.Text.Json.JsonElement be ? be.GetBoolean() : Convert.ToBoolean(req.Data?["BoolValue"]);
            
            Assert.Equal("test", stringVal);
            Assert.Equal(42, intVal);
            Assert.Equal(true, boolVal);

            return new IpcResponse
            {
                Success = true,
                CorrelationId = req.CorrelationId,
                Data = testData
            };
        });

        await _ipcService.StartServerAsync(_serviceProvider);

        var request = new IpcRequest
        {
            RequestType = "SerializationTest",
            Data = testData
        };

        // Act
        var response = await _ipcService.SendRequestAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        
        // Handle JsonElement in response
        var responseStringVal = response.Data["StringValue"] is System.Text.Json.JsonElement rse ? rse.GetString() : response.Data["StringValue"]?.ToString();
        Assert.Equal("test", responseStringVal);
    }

    [Fact]
    public void ServiceProvider_ResolvesServicesCorrectly()
    {
        // Arrange & Act
        var ipcService = _serviceProvider.GetService<IIpcService>();

        // Assert
        Assert.NotNull(ipcService);
        Assert.IsType<IpcService>(ipcService);
    }

    public void Dispose()
    {
        _ipcService.StopServerAsync().Wait();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}
