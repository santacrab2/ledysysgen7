using System;
using System.Linq;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;


//TwitchBot Instance/Namespace to call upon from forms
public class TwitchBot
{

    private readonly TwitchClient client;
    private readonly string Channel;


    public TwitchBot()
    {
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 100,
            ThrottlingPeriod = TimeSpan.FromSeconds(30),

            WhispersAllowedInPeriod = 100,
            WhisperThrottlingPeriod = TimeSpan.FromSeconds(60),

            // message queue capacity is managed (10_000 for message & whisper separately)
            // message send interval is managed (50ms for each message sent)
        };
        WebSocketClient WebCli = new(clientOptions);
        client = new TwitchClient(WebCli);
        var prefix = '!';
        var User = Ledybot.Program.f1.twuser.Text;
        var Token = Ledybot.Program.f1.twtoken.Text;
        Channel = Ledybot.Program.f1.twchannel.Text;
        var Credentials = new ConnectionCredentials(User, Token);

        client.Initialize(Credentials, Channel, prefix, prefix);
        client.OnDisconnected += Client_OnDisconnected;
        client.OnJoinedChannel += Client_onJoin;
        client.OnLeftChannel += Client_OnLeftChannel;

        client.Connect();

       
        
    }
    private void Client_onJoin(object sender, OnJoinedChannelArgs e)
    {

    }
    private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
    {

        while (!client.IsConnected)
            client.Reconnect();
    }

    private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
    {
        client.JoinChannel(e.Channel);
    }

    public void StartingDistribution()
    {
        Task.Run(async () =>
        {
            client.SendMessage(Channel,"Wonder trading in 15 seconds");
            await Task.Delay(15_000).ConfigureAwait(false);

            client.SendMessage(Channel, "3...");
            await Task.Delay(1_000).ConfigureAwait(false);
            client.SendMessage(Channel, "2...");
            await Task.Delay(1_000).ConfigureAwait(false);
            client.SendMessage(Channel, "1...");
            await Task.Delay(1_000).ConfigureAwait(false);

                client.SendMessage(Channel, "wonder trade now!");
        });

    }
}