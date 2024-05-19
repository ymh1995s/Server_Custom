using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        protected List<byte> _receivedData = new List<byte>();

        //한번에 최대로 받을수 있는 '소켓'의 버퍼
        const int MAXSIZE = 4096;
        byte[] socketBuffer = new byte[MAXSIZE];

        object _lock = new object();
        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnRecv(List<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract Session Clone();

        void Clear()
        {
            lock (_lock)
            {
                _receivedData.Clear();
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        // 성능 상 Send(ArraySegment<byte> sendBuff)보다 유리함
        // 전송에 지연이 발생
        // 아래 사용 예시 : 0.25초 마다 모아두었던 sendBuffList를 Send 처리 하기
        // List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        // for(int i=0; i<10; i++)
        //  _pendingList.Add(Arraysengment 데이터);

        // void Flush()
        // {
        //   Send(_pendingList);
        //   _pendingList.Clear();
        // }

        // while()
        // {
        //  Flush();
        //  Thread.Sleep(250);
        // }

        public void Send(List<byte[]> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            lock (_lock)
            {
                foreach (byte[] sendBuff in sendBuffList)
                    _sendQueue.Enqueue(sendBuff);

                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        // 적당한 부하, (가용 시) 즉시 전송
        public void Send(byte[] sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            if (_socket == null) return; //아직 연결된 세션이 없음
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            Clear();
        }

        public bool isCconnected()
        {
            if (_socket == null) return false;
            return _socket.Connected;
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            if (_disconnected == 1)
                return;

            while (_sendQueue.Count > 0)
            {
                byte[] buff = _sendQueue.Dequeue();
                ArraySegment<byte> tempbuf = new ArraySegment<byte>(buff);
                _pendingList.Add(tempbuf);
            }
            _sendArgs.BufferList = _pendingList;

            try
            {
                bool pending = _socket.SendAsync(_sendArgs); //데이터가 실제로 전송됨
                if (pending == false)
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            if (_disconnected == 1)
                return;

            _receivedData.Clear();

            _recvArgs.SetBuffer(socketBuffer, 0, socketBuffer.Length);

            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                //true : 보류, / false : 동기적으로 완료(즉시 연결 완료)
                if (pending == false)
                    OnRecvCompleted(null, _recvArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Failed {e}");
            }
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    //이번에 수신한 데이터
                    _receivedData = new List<byte>(args.Buffer);
                    int receiveSize = args.BytesTransferred;

                    //MAXBuffer 4096 중 실질적인 버퍼만 잘라 사용
                    OnRecv(_receivedData.GetRange(0, receiveSize));

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                    //버퍼 초기화 및 수신 대기 재등록
                    _receivedData.Clear();
                    RegisterRecv();
                }
            }
            else
            {
                Disconnect();
            }
        }

        #endregion
    }
}
