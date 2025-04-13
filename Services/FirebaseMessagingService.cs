using FirebaseAdmin.Messaging;
using System;
using System.Threading.Tasks;

public class FirebaseMessagingService
{
    private readonly FirebaseMessaging _messaging;

    public FirebaseMessagingService()
    {
        _messaging = FirebaseMessaging.DefaultInstance;
    }
    
    public async Task SendWorkoutReminder(string deviceToken, string title, string body)
    {
        var message = new Message
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            }
        };
        
        await _messaging.SendAsync(message);
    }
} 