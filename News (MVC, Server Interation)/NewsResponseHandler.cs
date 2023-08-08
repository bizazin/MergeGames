using Game.Signals.News;
using Newtonsoft.Json;
using Websockets.Requests.Params;
using Websockets.Responses;
using Websockets.Settings;
using Websockets.Values.News;
using Zenject;

namespace Websockets.ResponseHandlers.Impls.News
{
    public class NewsResponseHandler : AResponseHandler
    {
        public override string Key => WebSocketsResponseKeys.News;

        public NewsResponseHandler(SignalBus signalBus) : base(signalBus) { }

        public override void Handle(string message)
        {
            var messageVo =
                JsonConvert.DeserializeObject<Message<EmptyParams, NewsValue>>(message);

            var value = messageVo.result.response.value;

            if (value.status.IsSuccess)
                SignalBus.Fire(new SignalNewsInfo(value));
        }
    }
}