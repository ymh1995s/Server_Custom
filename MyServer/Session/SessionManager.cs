using ServerCore;
using System;

namespace MyServer
{
    public class SessionManager
    {
        //연결대상이 될 모든 장비 정의
        public enum TEquipment { SessionA_1, SessionA_2, SessionA_3, SessionA_4, SessionB, SessionC};

        //종료/재접속 시 원본 클래스를 가져오기 위한 세션 배열
        Session[] originSessions;
        //실질적으로 관리되는 세션 배열
        public Session[] sessions;
        
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        object _lock = new object();

        SessionManager()
        {
            originSessions = new Session[] { new SessionA_1(), new SessionA_2(), new SessionA_3(), new SessionB(), new SessionC()};
            sessions = new Session[originSessions.Length];

            for(int i = 0; i<originSessions.Length;i++)
            {
                sessions[i] = originSessions[i].Clone(); //원본 세션을 깊은 복사하여 원븐은 유지
            }
        }

        public object Generate(TEquipment equipNo)
        {
            lock (_lock)
            {
                return sessions[(int)equipNo];
            }
        }

        public void Delete(TEquipment equipNo)
        {
            lock (_lock)
            {
                sessions[(int)equipNo].Disconnect();
                sessions[(int)equipNo] = originSessions[(int)equipNo].Clone();//다시 원본 세션을 가져온다.
            }
        }

        public void Send(TEquipment equipNo, byte[] data)
        {
            if (sessions[(int)equipNo] != null)
                sessions[(int)equipNo].Send(data);
        }
    }
}
