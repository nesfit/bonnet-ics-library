using System.Net;
namespace SampleScenes
{
    public record ConnectionInformation(IPEndPoint endPoint, byte deviceId)
    {
    }
}