using Game.Signals.Poll;
using Newtonsoft.Json;
using Websockets.Requests.Params.Poll;
using Websockets.Responses;
using Websockets.Settings;
using Websockets.Values.Poll;
using Zenject;

namespace Websockets.ResponseHandlers.Impls.Poll
{
    public class PollVotesResponseHandler : AResponseHandler
    {
        public override string Key => WebSocketsResponseKeys.PollVotes;
        
        public PollVotesResponseHandler(SignalBus signalBus) : base(signalBus) { }

        public override void Handle(string message)
        {
            var messageVo =
                JsonConvert.DeserializeObject<Message<PollVotesParams, PollVotesValue>>(message);
            
            var value = messageVo.result.response.value;

            SignalBus.Fire(new SignalPollVotes(value, value.status.IsSuccess));
        }
    }
}