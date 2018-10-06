using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using BebopCommandSet;


namespace drone_UDP
{
    public class BebopCommand
    {
        private readonly int[] _seq = new int[256];
        private PCMD _pcmd;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationToken _cancelToken;

        private UdpClient _arstreamClient;
        private IPEndPoint _remoteIpEndPoint;

        private UdpClient _d2CClient;

        private static readonly object ThisLock = new object();


        //int frameCount = 0;


        public int Discover()
        {
            Console.WriteLine("Discovering...");

            _d2CClient = new UdpClient(CommandSet.IP, 54321);


            //make handshake with TCP_client, and the port is set to be 4444
            using (var tcpClient = new TcpClient(CommandSet.IP, CommandSet.DISCOVERY_PORT))
            {
                var stream = new NetworkStream(tcpClient.Client);

                //initialize reader and writer
                StreamWriter streamWriter = new StreamWriter(stream);
                StreamReader streamReader = new StreamReader(stream);

                //when the drone receive the message bellow, it will return the confirmation
                string handshakeMessage =
                    "{\"controller_type\":\"computer\", \"controller_name\":\"halley\", \"d2c_port\":\"43210\", \"arstream2_client_stream_port\":\"55004\", \"arstream2_client_control_port\":\"55005\"}";
                streamWriter.WriteLine(handshakeMessage);
                streamWriter.Flush();


                string receiveMessage = streamReader.ReadLine();
                if (receiveMessage == null)
                {
                    Console.WriteLine("Discover failed");
                    return -1;
                }
                else
                {
                    Console.WriteLine("The message from the drone shows: " + receiveMessage);

                    //initialize
                    _pcmd = default(PCMD);

                    //All State setting
                    GenerateAllStates();
                    GenerateAllSettings();

                    //enable video streaming
                    VideoEnable();

                    //init ARStream
                    //initARStream();

                    //init CancellationToken
                    _cancelToken = _cts.Token;


                    PcmdThreadActive();
                    //arStreamThreadActive();
                    return 1;
                }
            }
        }


        public void SendCommandAdpator(ref Command cmd, int type = CommandSet.ARNETWORKAL_FRAME_TYPE_DATA,
            int id = CommandSet.BD_NET_CD_NONACK_ID)
        {
            int bufSize = cmd.size + 7;
            byte[] buf = new byte[bufSize];

            _seq[id]++;
            if (_seq[id] > 255) _seq[id] = 0;

            buf[0] = (byte) type;
            buf[1] = (byte) id;
            buf[2] = (byte) _seq[id];
            buf[3] = (byte) (bufSize & 0xff);
            buf[4] = (byte) ((bufSize & 0xff00) >> 8);
            buf[5] = (byte) ((bufSize & 0xff0000) >> 16);
            buf[6] = (byte) ((bufSize & 0xff000000) >> 24);

            cmd.cmd.CopyTo(buf, 7);


            _d2CClient.Send(buf, buf.Length);
        }

        public void Takeoff()
        {
            Console.WriteLine("try to takeoff ing...");
            var cmd = default(Command);
            cmd.size = 4;
            cmd.cmd = new byte[4];

            cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_ARDRONE3;
            cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_ARDRONE3_CLASS_PILOTING;
            cmd.cmd[2] = CommandSet.ARCOMMANDS_ID_ARDRONE3_PILOTING_CMD_TAKEOFF;
            cmd.cmd[3] = 0;

            SendCommandAdpator(ref cmd, CommandSet.ARNETWORKAL_FRAME_TYPE_DATA_WITH_ACK, CommandSet.BD_NET_CD_ACK_ID);
        }

        public void Landing()
        {
            Console.WriteLine("try to landing...");
            var cmd = default(Command);
            cmd.size = 4;
            cmd.cmd = new byte[4];

            cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_ARDRONE3;
            cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_ARDRONE3_CLASS_PILOTING;
            cmd.cmd[2] = CommandSet.ARCOMMANDS_ID_ARDRONE3_PILOTING_CMD_LANDING;
            cmd.cmd[3] = 0;

            SendCommandAdpator(ref cmd, CommandSet.ARNETWORKAL_FRAME_TYPE_DATA_WITH_ACK, CommandSet.BD_NET_CD_ACK_ID);
        }

        public void Move(int flag, int roll, int pitch, int yaw, int gaz)
        {
            _pcmd.flag = flag;
            _pcmd.roll = roll;
            _pcmd.pitch = pitch;
            _pcmd.yaw = yaw;
            _pcmd.gaz = gaz;

            /*var task = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("move thread start");
                generatePCMD(flag, roll, pitch, yaw, gaz);
            }, cancelToken);
            task.Wait();
            Console.WriteLine("move thread end");
            task.Dispose();*/
        }

        public void GeneratePcmd()
        {
            lock (ThisLock)
            {
                var cmd = default(Command);
                cmd.size = 13;
                cmd.cmd = new byte[13];

                cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_ARDRONE3;
                cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_ARDRONE3_CLASS_PILOTING;
                cmd.cmd[2] = CommandSet.ARCOMMANDS_ID_ARDRONE3_PILOTING_CMD_PCMD;
                cmd.cmd[3] = 0;

                cmd.cmd[4] = (byte) _pcmd.flag; // flag
                cmd.cmd[5] =
                    (_pcmd.roll >= 0)
                        ? (byte) _pcmd.roll
                        : (byte) (256 + _pcmd.roll); // roll: fly left or right [-100 ~ 100]
                cmd.cmd[6] =
                    (_pcmd.pitch >= 0)
                        ? (byte) _pcmd.pitch
                        : (byte) (256 + _pcmd.pitch); // pitch: backward or forward [-100 ~ 100]
                cmd.cmd[7] =
                    (_pcmd.yaw >= 0)
                        ? (byte) _pcmd.yaw
                        : (byte) (256 + _pcmd.yaw); // yaw: rotate left or right [-100 ~ 100]
                cmd.cmd[8] =
                    (_pcmd.gaz >= 0) ? (byte) _pcmd.gaz : (byte) (256 + _pcmd.gaz); // gaze: down or up [-100 ~ 100]


                // for Debug Mode
                cmd.cmd[9] = 0;
                cmd.cmd[10] = 0;
                cmd.cmd[11] = 0;
                cmd.cmd[12] = 0;

                SendCommandAdpator(ref cmd);
            }
        }

        public void PcmdThreadActive()
        {
            Console.WriteLine("The PCMD thread is starting");

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    GeneratePcmd();
                    Thread.Sleep(50); //sleep 50ms each time.
                }
            }, _cancelToken);
        }

        public void CancleAllTask()
        {
            _cts.Cancel();
            //Console.WriteLine(frameCount);

            _d2CClient.Close(); // Disposes
        }

        public void GenerateAllStates()
        {
            Console.WriteLine("Generate All State");
            var cmd = default(Command);
            cmd.size = 4;
            cmd.cmd = new byte[4];

            cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_COMMON;
            cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_COMMON_CLASS_COMMON;
            cmd.cmd[2] = (CommandSet.ARCOMMANDS_ID_COMMON_COMMON_CMD_ALLSTATES & 0xff);
            cmd.cmd[3] = (CommandSet.ARCOMMANDS_ID_COMMON_COMMON_CMD_ALLSTATES & 0xff00 >> 8);

            SendCommandAdpator(ref cmd, CommandSet.ARNETWORKAL_FRAME_TYPE_DATA_WITH_ACK, CommandSet.BD_NET_CD_ACK_ID);
        }

        public void GenerateAllSettings()
        {
            Console.WriteLine("Generate All Settings");
            var cmd = default(Command);
            cmd.size = 4;
            cmd.cmd = new byte[4];

            cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_COMMON;
            cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_COMMON_CLASS_SETTINGS;
            cmd.cmd[2] = (0 & 0xff); // ARCOMMANDS_ID_COMMON_CLASS_SETTINGS_CMD_ALLSETTINGS = 0
            cmd.cmd[3] = (0 & 0xff00 >> 8);

            SendCommandAdpator(ref cmd, CommandSet.ARNETWORKAL_FRAME_TYPE_DATA_WITH_ACK, CommandSet.BD_NET_CD_ACK_ID);
        }

        public void VideoEnable()
        {
            Console.WriteLine("Send Video Enable Command");
            var cmd = default(Command);
            cmd.size = 5;
            cmd.cmd = new byte[5];

            cmd.cmd[0] = CommandSet.ARCOMMANDS_ID_PROJECT_ARDRONE3;
            cmd.cmd[1] = CommandSet.ARCOMMANDS_ID_ARDRONE3_CLASS_MEDIASTREAMING;
            cmd.cmd[2] = (0 & 0xff); // ARCOMMANDS_ID_COMMON_CLASS_SETTINGS_CMD_VIDEOENABLE = 0
            cmd.cmd[3] = (0 & 0xff00 >> 8);
            cmd.cmd[4] = 1; //arg: Enable

            SendCommandAdpator(ref cmd, CommandSet.ARNETWORKAL_FRAME_TYPE_DATA_WITH_ACK, CommandSet.BD_NET_CD_ACK_ID);
        }

        public void InitArStream()
        {
            _arstreamClient = new UdpClient(55004);
            _remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public void GetImageData()
        {
            //Console.WriteLine("Receiving...");

            var receivedData = _arstreamClient.Receive(ref _remoteIpEndPoint);
            Console.WriteLine("Receive Data: " + BitConverter.ToString(receivedData));
            //frameCount++;
            //arstreamClient.BeginReceive(new AsyncCallback(recvData), null);
        }


        public void ArStreamThreadActive()
        {
            Console.WriteLine("The ARStream thread is starting");

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    //Thread.Sleep(1000);
                    GetImageData();
                }
            }, _cancelToken);
        }
    }
}