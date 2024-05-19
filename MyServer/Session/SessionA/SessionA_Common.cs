
using System.Collections.Generic;

namespace MyServer
{
    static class SessionA_Common
    {
        static public void Parsing(List<byte> SessionBuffer, byte[] receivedBuffer, SessionManager.TEquipment band)
        {
            //기본 검증
            if (receivedBuffer == null) return;
            if (receivedBuffer.Length == 0) return;

            //<>꼴이 배열로 들어감 ~= byte[][]
            List<byte[]> processedPatterns = new List<byte[]>();

            //List에 수신한 데이터 추가
            SessionBuffer.AddRange(receivedBuffer);

            int startIndex = 0;
            //수신한 데이터로부터 유효한 <>꼴을 모두 추출
            while (startIndex < SessionBuffer.Count)
            {
                int startPatternIndex = SessionBuffer.IndexOf((byte)'<', startIndex);
                int endPatternIndex = SessionBuffer.IndexOf((byte)'>', startIndex);

                //완성되지 않은 메시지
                if (startPatternIndex == -1 || endPatternIndex == -1)
                    break;

                // 첫 연결 시 데이터가 끊겨서 오는 경우, 그 데이터는 버리고 다음 데이터부터 수신
                // ex) 
                // ta, Data, Data>
                // <MSG,Data,Data,Data,Data,Data>
                // 꼴의 데이터를 먼저 받을 시 예외 처리
                if (endPatternIndex < startPatternIndex)
                {
                    SessionBuffer.Clear();
                    break;
                }

                //1개의 <>꼴 메시지 추출
                int MSGLength = endPatternIndex - startPatternIndex + 1;
                byte[] oneMSG = SessionBuffer.GetRange(startPatternIndex, MSGLength).ToArray();

                if (oneMSG[0] == (byte)'<' && oneMSG[MSGLength - 1] == (byte)'>')
                {
                    //추출한 크기 만큼 삭제 및 시작 배열 초기화
                    SessionBuffer.RemoveRange(0, endPatternIndex + 1);
                    startIndex = 0;
                }
                else
                {
                    //유효하지 않은 패턴
                    startIndex = endPatternIndex + 1;
                }
            }

        }
    }

}