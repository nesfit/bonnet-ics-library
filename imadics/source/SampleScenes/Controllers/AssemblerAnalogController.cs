using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SoftControllers;
using FTRIG = SoftControllers.FallingEdgeDetector; 
using RTRIG = SoftControllers.RaisingEdgeDetector;
using TON = SoftControllers.OnDelayTimer; 
namespace SampleScenes.Controllers
{
    public class AssemblerAnalogController : ModbusController
    {
        Screen _screen;

        #region I/O points
        // OUTPUT
        Coil lidsConveyor1;
        Coil lidsConveyor2;
        Coil stopBlade1;

        Coil basesConveyor1;
        Coil basesConveyor2;
        Coil stopBlade2;

        HoldingRegister setX;
        HoldingRegister setZ;
        Coil grab;
        HoldingRegister counter;

        // INPUT:
        DiscreteInput lidAtPlace;
        DiscreteInput baseAtPlace;
        DiscreteInput partLeaving;
        DiscreteInput itemDetected;
        InputRegister positionX;
        InputRegister positionZ;

        RTRIG rtLidAtPlace = new RTRIG();
        RTRIG rtBaseAtPlace = new RTRIG();
        FTRIG ftPartLeaving = new FTRIG();

        State stateLids = State.State0;
        State stateBases = State.State0;

        TON grabTimer = new TON();
        TON watchDogTimer = new TON(); 

        #endregion


        public AssemblerAnalogController(ConnectionInformation connection, RegisterMap registerMap, Screen screen) 
            : base(connection.endPoint, connection.deviceId, new StreamWriter($"Log_{nameof(AssemblerAnalogController)}.csv"))
        {
            _screen = screen;
            this.CycleTime = 50;
            // create I/O points
            lidsConveyor1 = registerMap.GetCoil(Device,"Lids conveyor 1");
            lidsConveyor2 = registerMap.GetCoil(Device,"Lids conveyor 2");
            stopBlade1 = registerMap.GetCoil(Device,"Stop blade 1");

            basesConveyor1 = registerMap.GetCoil(Device,"Bases conveyor 1");
            basesConveyor2 = registerMap.GetCoil(Device,"Bases conveyor 2");
            stopBlade2 = registerMap.GetCoil(Device,"Stop blade 2");

            setX = registerMap.GetHoldingRegister(Device,"Set X");
            setZ = registerMap.GetHoldingRegister(Device,"Set Z");
            grab = registerMap.GetCoil(Device,"Grab");
            counter = registerMap.GetHoldingRegister(Device,"Counter");

            lidAtPlace = registerMap.GetDiscreteInput(Device,"Lid at place");
            baseAtPlace = registerMap.GetDiscreteInput(Device,"Base at place");
            partLeaving = registerMap.GetDiscreteInput(Device,"Part leaving");
            itemDetected = registerMap.GetDiscreteInput(Device,"Item detected");
            positionX = registerMap.GetInputRegister(Device,"X");
            positionZ = registerMap.GetInputRegister(Device,"Z");

            AutoRegisterDatapoints(this);       
        }

        protected override Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel)
        {
            rtLidAtPlace.CLK(lidAtPlace.Value);

            if (stateLids == State.State0)
            {
                lidsConveyor1.Value = true;
                stopBlade1.Value = true;

                setX.FloatValue = 1.1f;

                if (rtLidAtPlace.Q)
                    stateLids = State.State1;
            }
            else if (stateLids == State.State1)
            {
                lidsConveyor1.Value = false;
                setZ.FloatValue = 9f;

                if (itemDetected.Value)
                {
                    grabTimer.PT = 1000;
                    grabTimer.IN = true;

                    stateLids = State.State2;
                }
            }
            else if (stateLids == State.State2)
            {
                grab.Value = true;

                if (grabTimer.Q)
                {
                    grabTimer.IN = false;

                    stateLids = State.State3;
                }
            }
            else if (stateLids == State.State3)
            {
                setZ.Value = 0;
                setX.FloatValue = 8.85f;

                if (positionX.FloatValue > 8.80f)
                {
                    watchDogTimer.PT = 3000;
                    watchDogTimer.IN = true;

                    stateLids = State.State4;
                }
            }
            else if (stateLids == State.State4)
            {
                setZ.FloatValue = 10f;
                stopBlade2.Value = false;

                watchDogTimer.IN = true;

                if (watchDogTimer.Q || positionZ.FloatValue > 8.4f)
                {
                    if (!watchDogTimer.Q)
                        counter.Value++;

                    watchDogTimer.IN = false;

                    stateLids = State.State5;
                }
            }
            else if (stateLids == State.State5)
            {
                setZ.Value = 0;
                setX.Value = 0;
                grab.Value = false;

                if (stateBases != State.State0)
                    stateBases = State.State2;

                if (positionX.FloatValue < 0.1f && positionZ.FloatValue < 0.1f)
                    stateLids = State.State0;
            }

            //Bases
            rtBaseAtPlace.CLK(baseAtPlace.Value);
            ftPartLeaving.CLK(partLeaving.Value);

            if (stateBases == State.State0)
            {
                basesConveyor1.Value = true;
                basesConveyor2.Value = false;
                stopBlade2.Value = true;

                if (rtBaseAtPlace.Q)
                    stateBases = State.State1;
            }
            else if (stateBases == State.State1)
            {
                basesConveyor1.Value = false;
            }
            else if (stateBases == State.State2)
            {
                stopBlade2.Value = false;

                basesConveyor1.Value = true;
                basesConveyor2.Value = true;

                if (ftPartLeaving.Q)
                    stateBases = State.State0;
            }
            
            _ = Task.Run(() => UpdateUI(elapsedMilliseconds));
            return Task.CompletedTask;
        }

        protected override Task FinalizeAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task InitializeAsync(CancellationToken cancel)
        {
            lidsConveyor1.Value = false;
            lidsConveyor2.Value = false;

            stopBlade1.Value = false;
            stopBlade2.Value = false;

            basesConveyor1.Value = true;
            basesConveyor2.Value = true;

            setX.Value = 0;
            setZ.Value = 0;
            grab.Value = false;

            counter.Value = 0;

            _screen.MoveAddString (5, 2, $"=== CONTROLLER ===");
            _screen.MoveAddString (6, 2, $"Assembler (Analog) Controller, connected to {Device.EndPoint}/{Device.DeviceId}.");
            _screen.Refresh ();

            return Task.CompletedTask;
        }

        ElapsedTimeObserver elapsedObserver = new ElapsedTimeObserver(50);

         private void UpdateUI (int elapsedMilliseconds) 
        {
            elapsedObserver.Add(elapsedMilliseconds);
            _screen.MoveAddString (7, 2, $"Running time = {elapsedObserver.RunningTime}");
            _screen.MoveAddString (8, 2, $"Cycle time: avg10 = {elapsedObserver.MovingAvg}, max = {elapsedObserver.Max}, min = {elapsedObserver.Min}");       

            _screen.MoveAddString (10, 2, $"== STATES ==");
            _screen.MoveAddString (11, 2, $"Lids State = {stateLids.ToString()}   ");
            _screen.MoveAddString (12, 2, $"Bases State = {stateBases.ToString()}   ");


            _screen.MoveAddString (10, 32, $"== SENSORS ==");
            _screen.MoveAddString (11, 32, $"{lidAtPlace.ToString()}    ");
            _screen.MoveAddString (12, 32, $"{baseAtPlace.ToString()}    ");

            _screen.MoveAddString (13, 32, $"{partLeaving.ToString()}    ");
            _screen.MoveAddString (14, 32, $"{itemDetected.ToString()}    ");

            _screen.MoveAddString (16, 32, $"{positionX.ToString()}    ");
            _screen.MoveAddString (17, 32, $"{positionZ.ToString()}    ");

            _screen.MoveAddString (10, 82, $"== ACTUATORS ==");

            _screen.MoveAddString (12, 82, $"{lidsConveyor1.ToString()}    ");
            _screen.MoveAddString (13, 82, $"{lidsConveyor2.ToString()}    ");
            _screen.MoveAddString (14, 82, $"{stopBlade1.ToString()}    ");

            _screen.MoveAddString (16, 82, $"{basesConveyor1.ToString()}    ");
            _screen.MoveAddString (17, 82, $"{basesConveyor2.ToString()}    ");
            _screen.MoveAddString (18, 82, $"{stopBlade2.ToString()}    ");

            _screen.MoveAddString (20, 82, $"{setX.ToString()}    ");
            _screen.MoveAddString (21, 82, $"{setZ.ToString()}    ");
            _screen.MoveAddString (22, 82, $"{grab.ToString()}    ");

            _screen.MoveAddString (24, 82, $"{counter.ToString()}    ");
            _screen.Refresh ();
        }

    }
}