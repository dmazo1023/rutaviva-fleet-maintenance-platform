namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public static class SubscribingTestNames
{
    private const string GuidFormat = "N";
    private const string StreamPrefix = "testing.subscribing.";
    private const string SubjectPrefix = "subject/";
    private const string SubscriberPrefix = "subscriber-";

    public static string NewStream() => StreamPrefix + NewSuffix();

    public static string NewSubject() => SubjectPrefix + NewSuffix();

    public static string NewSubscriber() => SubscriberPrefix + NewSuffix();

    private static string NewSuffix() => Guid.NewGuid().ToString(GuidFormat);
}
