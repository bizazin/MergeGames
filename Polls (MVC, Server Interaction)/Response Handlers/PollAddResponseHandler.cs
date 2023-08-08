using Game.Signals.Poll;
using Newtonsoft.Json;
using Websockets.Requests.Params.Poll;
using Websockets.Responses;
using Websockets.Settings;
using Websockets.Values.Poll;
using Zenject;

namespace Websockets.ResponseHandlers.Impls.Poll
{
     public class PollAddResponseHandler : AResponseHandler
     {
         public override string Key => WebSocketsResponseKeys.Poll;
         
         public PollAddResponseHandler(SignalBus signalBus) : base(signalBus) { }

         public override void Handle(string message)
         {
             var messageVo =
                 JsonConvert.DeserializeObject<Message<PollParams, PollAddValue>>(message);
             
             var value = messageVo.result.response.value;
             
             SignalBus.Fire(new SignalPollAdd(value));
         }
     }
}