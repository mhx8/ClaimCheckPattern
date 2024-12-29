using Microsoft.AspNetCore.Mvc;

namespace Publisher;

[ApiController]
[Route("api/events")]
public class EventsController(
    EventPublisher eventPublisher,
    ILogger<EventsController> logger) : ControllerBase
{
    [HttpGet("{message}")]
    public async Task<IActionResult> SendEvent(
        string message)
    {
        await eventPublisher.SendEventAsync(message);
        
        return Ok("Event sent! 🚀");
    }
}