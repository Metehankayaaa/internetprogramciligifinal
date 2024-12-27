using Microsoft.AspNetCore.SignalR;

namespace WebUı.Hubs
{
    public class CommentHub : Hub
    {
        public async Task SendComment(string userName, string comment, string date, string newsTitle)
        {
            await Clients.All.SendAsync("ReceiveComment", userName, comment, date, newsTitle);
        }
    }
}
