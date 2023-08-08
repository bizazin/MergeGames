using Game.Signals.Poll;
using Newtonsoft.Json;
using Websockets.Requests.Params;
using Websockets.Responses;
using Websockets.Settings;
using Websockets.Values.Poll;
using Zenject;

namespace Websockets.ResponseHandlers.Impls.Poll
{
    public class PollHistoryResponseHandler: AResponseHandler
    {
        public override string Key => WebSocketsResponseKeys.PollHistory;
        
        public PollHistoryResponseHandler(SignalBus signalBus) : base(signalBus) { }

        public override void Handle(string message)
        {
            var messageVo =
                JsonConvert.DeserializeObject<Message<EmptyParams, PollAllHistoryValue>>(message);
            
            var value = messageVo.result.response.value;

            SignalBus.Fire(new SignalPollHistory(value));
        }
        
    }
}