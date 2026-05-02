using Amazon.Lambda.Core;

namespace Oficina.AuthLambda.Tests.TestSupport;

internal sealed class TestLambdaContext : ILambdaContext
{
    public string AwsRequestId => "request-id";
    public IClientContext ClientContext => null!;
    public string FunctionName => "test";
    public string FunctionVersion => "$LATEST";
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn => "arn:aws:lambda:us-east-1:000000000000:function:test";
    public ILambdaLogger Logger { get; } = new TestLambdaLogger();
    public string LogGroupName => "/aws/lambda/test";
    public string LogStreamName => "stream";
    public int MemoryLimitInMB => 256;
    public TimeSpan RemainingTime => TimeSpan.FromSeconds(30);
}

internal sealed class TestLambdaLogger : ILambdaLogger
{
    public List<string> Lines { get; } = [];

    public void Log(string message) => Lines.Add(message);

    public void LogLine(string message) => Lines.Add(message);
}
