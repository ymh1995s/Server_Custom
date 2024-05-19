using System;
using System.Collections.Generic;
using ServerCore;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace MyServer
{
    class SessionB : Session
    {
        //각각 세션의 수신 데이터 포멧이 다르므로 End단의 전용 버퍼 추가
        private List<byte> endSessionReceivedData = new List<byte>();

        object _lock = new object();

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            endSessionReceivedData.Clear();
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Msg Send Complete, Msg Size is : { numOfBytes }");
        }

        // buffer : 이번에 수신한 데이터(뭉치)
        public sealed override void OnRecv(List<byte> buffer)
        {
            byte[] data = buffer.ToArray();
            byte[][] send_data = null;

            //삭제 : buffer To send_data

            if (send_data.Length == 0) return;

            //삭제 : SendToEachSession();
        }

        public override Session Clone()
        {
            return new SessionB();
        }
    }
}
