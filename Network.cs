using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Trax
{
    public enum NetworkContentType
    {
        net_proto_version = 0,
        scenery_name = 1,
        event_call = 2,
        ai_command = 3,
        track_occupancy = 4,
        isolated_occupancy = 5,
        param_set = 6,
        param_set_pause = 1,
        param_set_time = 2,
        vehicle_ask = 7,
        vehicle_params = 8,
        vehicle_damage = 9,
        signal_state = 10,
    }

    public enum NetworkMessageType
    {
        ask = 0,
        answer = 1,
        info = 2
    }

    class Network
    {
        private static NLog.Logger log;
        private static DealerSocket socket;
        private Main main;
        public Network(ref DealerSocket s, Main m)
        {
            log = log = NLog.LogManager.GetCurrentClassLogger();
            socket = s;
            main = m;
        }

        public void HandleMessage(object s, NetMQSocketEventArgs a)
        {
            var msg = a.Socket.ReceiveMultipartMessage();
            log.Info("odebrano pakiet sieciowy");
            if (msg.Count() < 3)
                return; // są minimum trzy klatki - identyfikator, typ wiadomości, wiadomość



            switch ((NetworkMessageType)msg[1].ConvertToInt32()) {
                case NetworkMessageType.answer:
                    HandleAnswer(msg); break;
            }

        }

        public void AskForScenery() {
            var msg = new NetMQMessage();
            msg.Append((int)NetworkMessageType.ask);
            msg.Append((int)NetworkContentType.scenery_name);
            socket.SendMultipartMessage(msg);
        }

        private void HandleAnswer(NetMQMessage msg) {
            switch ((NetworkContentType)msg[1].ConvertToInt32()) {
                case NetworkContentType.net_proto_version:
                    if (msg.FrameCount != 4)
                        log.Info("Protocol versio: {0} is {1}", msg[3].ConvertToInt32());
                    break;
                case NetworkContentType.scenery_name:
                    if (msg.FrameCount != 4)
                        break;
                    log.Info("Scenery name: {0}", msg[3].ConvertToString());
                    break;
                case NetworkContentType.event_call:
                case NetworkContentType.ai_command:
                    log.Info("Remote procedures call");
                    break;
                case NetworkContentType.track_occupancy:
                    if (msg.FrameCount < 4 && (msg[3].ConvertToInt32() * 3 + 4) != msg.FrameCount)
                        break;
                    log.Info("Track occupancy: {0} is {1}", msg[4].ConvertToString(), msg[5].ConvertToInt32().ToString());
                    break;
                case NetworkContentType.isolated_occupancy:
                    if (msg.FrameCount < 4 && (msg[3].ConvertToInt32() * 2 + 4) != msg.FrameCount)
                        break;
                    log.Info("isolated occupancy: {0} is {1}", msg[4].ConvertToString(), msg[5].ConvertToInt32().ToString());
                    break;
                case NetworkContentType.param_set:
                case NetworkContentType.vehicle_ask:
                case NetworkContentType.vehicle_damage:
                case NetworkContentType.vehicle_params:
                    log.Info("Params and damage");
                    break;
                default:
                    log.Info("Network code not known");
                    break;
            }
        }

    }
}
