using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SoftControllers;

namespace SampleScenes.Controllers
{
    public class AssemblerController : ModbusController
    {
        Coil lidsConveyor;
        Coil moveX;
        Coil moveZ;
        Coil grab;
        Coil basesConveyor;
        Coil clampLid;
        Coil posRaiseLid;
        Coil clampBase;
        Coil posRaiseBase;
        HoldingRegister counter;

        DiscreteInput lidAtPlace;
        DiscreteInput baseAtPlace;
        DiscreteInput partLeaving;
        DiscreteInput itemDetected;
        DiscreteInput movingZ;
        DiscreteInput movingX;
        DiscreteInput lidClamped;
        DiscreteInput posAtLimitLids;
        DiscreteInput baseClamped;
        DiscreteInput posAtLimitBases;

        FallingEdgeDetector ftLidAtPlace = new FallingEdgeDetector();
        FallingEdgeDetector ftBaseAtPlace = new FallingEdgeDetector();
        FallingEdgeDetector ftMovingZ = new FallingEdgeDetector();
        FallingEdgeDetector ftMovingX = new FallingEdgeDetector();
        RaisingEdgeDetector rtMxmz = new RaisingEdgeDetector();
        FallingEdgeDetector ftPartLeaving = new FallingEdgeDetector();

        State stateLids = State.State0;
        State stateBases = State.State0;


        Screen _screen;

        public AssemblerController(ConnectionInformation connection, RegisterMap registerMap, Screen screen) 
            : base(connection.endPoint, connection.deviceId, new StreamWriter($"Log_{nameof(AssemblerController)}.csv"))
        {
            this._screen = screen;
            this.CycleTime = 50;

            lidsConveyor = registerMap.GetCoil(Device,"Lids conveyor");
            moveX = registerMap.GetCoil(Device,"Move X");
            moveZ = registerMap.GetCoil(Device,"Move Z");
            grab = registerMap.GetCoil(Device,"Grab");
            basesConveyor = registerMap.GetCoil(Device,"Bases conveyor");
            clampLid = registerMap.GetCoil(Device,"Clamp lid");
            posRaiseLid = registerMap.GetCoil(Device,"Pos. raise (lids)");
            clampBase = registerMap.GetCoil(Device,"Clamp base");
            posRaiseBase = registerMap.GetCoil(Device,"Pos. raise (bases)");

            counter = registerMap.GetHoldingRegister(Device, "Counter");

            lidAtPlace = registerMap.GetDiscreteInput(Device,"Lid at place");
            baseAtPlace = registerMap.GetDiscreteInput(Device,"Base at place");
            partLeaving = registerMap.GetDiscreteInput(Device,"Part leaving");
            itemDetected = registerMap.GetDiscreteInput(Device,"Item detected");
            movingZ = registerMap.GetDiscreteInput(Device,"Moving Z");
            movingX = registerMap.GetDiscreteInput(Device,"Moving X");
            lidClamped = registerMap.GetDiscreteInput(Device,"Lid clamped");
            posAtLimitLids = registerMap.GetDiscreteInput(Device,"Pos. at limit (lids)");
            baseClamped = registerMap.GetDiscreteInput(Device,"Base clamped");
            posAtLimitBases = registerMap.GetDiscreteInput(Device,"Pos. at limit (bases)");

            AutoRegisterDatapoints(this);
        }

        protected override Task FinalizeAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task InitializeAsync(CancellationToken cancel)
        {
            basesConveyor.Value = true;
            moveZ.Value = false;
            moveX.Value = false;
            grab.Value = false;
            clampLid.Value = false;
            posRaiseLid.Value = false;
            clampBase.Value = false;
            posRaiseBase.Value = false;

            counter.Value = 0;

            _screen.MoveAddString (5, 2, $"=== CONTROLLER ===");
            _screen.MoveAddString (6, 2, $"Assembler Controller, connected to {Device.EndPoint}/{Device.DeviceId}.");
            _screen.Refresh ();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes single control actions. 
        /// <para>
        /// This code was adapted from:
        /// https://github.com/realgamessoftware/factoryio-sdk/blob/master/samples/Controllers/Scenes/Assembler.cs
        /// </para>
        /// </summary>
        protected override Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel)
        {
             ftLidAtPlace.CLK(lidAtPlace.Value);
            ftMovingZ.CLK(movingZ.Value);
            ftMovingX.CLK(movingX.Value);
            rtMxmz.CLK(movingZ.Value && movingX.Value);

            //Lids
            if (stateLids == State.State0)
            {
                lidsConveyor.Value = true;

                if (ftLidAtPlace.Q)
                    stateLids = State.State1;
            }
            else if (stateLids == State.State1)
            {
                lidsConveyor.Value = false;
                clampLid.Value = true;

                if (lidClamped.Value)
                    stateLids = State.State2;
            }
            else if (stateLids == State.State2)
            {
                moveZ.Value = true;

                if (ftMovingZ.Q)
                    stateLids = State.State3;
            }
            else if (stateLids == State.State3)
            {
                if (itemDetected.Value)
                    stateLids = State.State4;
            }
            else if (stateLids == State.State4)
            {
                grab.Value = true;
                moveZ.Value = false;

                clampLid.Value = false;

                if (ftMovingZ.Q)
                    stateLids = State.State5;
            }
            else if (stateLids == State.State5)
            {
                moveX.Value = true;

                if (ftMovingX.Q)
                    stateLids = State.State6;
            }
            else if (stateLids == State.State6)
            {
                moveZ.Value = true;

                if (ftMovingZ.Q)
                    stateLids = State.State7;
            }
            else if (stateLids == State.State7)
            {
                grab.Value = false;
                moveZ.Value = false;

                stateBases = State.State2;

                if (ftMovingZ.Q && !itemDetected.Value)
                {
                    counter.Value++;

                    stateLids = State.State8;
                }
            }
            else if (stateLids == State.State8)
            {
                if (!itemDetected.Value)
                    stateLids = State.State9;     
            }
            else if (stateLids == State.State9)
            {
                moveX.Value = false;

                if (ftMovingX.Q)
                    stateLids = State.State0;
            }

            //Bases
            ftBaseAtPlace.CLK(baseAtPlace.Value);
            ftPartLeaving.CLK(partLeaving.Value);

            if (stateBases == State.State0)
            {
                basesConveyor.Value = true;
                posRaiseBase.Value = false;

                if (ftBaseAtPlace.Q)
                    stateBases = State.State1;
            }
            else if (stateBases == State.State1)
            {
                basesConveyor.Value = false;
                clampBase.Value = true;
            }
            else if (stateBases == State.State2)
            {
                basesConveyor.Value = true;
                clampBase.Value = false;
                posRaiseBase.Value = true;

                if (ftPartLeaving.Q || baseAtPlace.Value)
                    stateBases = State.State0;
            }
            _ = Task.Run(() => UpdateUI(elapsedMilliseconds));
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

            _screen.MoveAddString (16, 32, $"{movingZ.ToString()}    ");
            _screen.MoveAddString (17, 32, $"{movingX.ToString()}    ");

            _screen.MoveAddString (19, 32, $"{lidClamped.ToString()}    ");
            _screen.MoveAddString (20, 32, $"{posAtLimitLids.ToString()}    ");
            
            _screen.MoveAddString (21, 32, $"{baseClamped.ToString()}    ");
            _screen.MoveAddString (22, 32, $"{posAtLimitBases.ToString()}    ");

            _screen.MoveAddString (10, 82, $"== ACTUATORS ==");
            _screen.MoveAddString (12, 82, $"{moveX.ToString()}    ");
            _screen.MoveAddString (13, 82, $"{moveZ.ToString()}    ");
            _screen.MoveAddString (14, 82, $"{grab.ToString()}    ");

            _screen.MoveAddString (16, 82, $"{lidsConveyor.ToString()}    ");
            _screen.MoveAddString (17, 82, $"{clampLid.ToString()}    ");
            _screen.MoveAddString (18, 82, $"{posRaiseLid.ToString()}    ");

            _screen.MoveAddString (20, 82, $"{basesConveyor.ToString()}    ");
            _screen.MoveAddString (21, 82, $"{clampBase.ToString()}    ");
            _screen.MoveAddString (22, 82, $"{posRaiseBase.ToString()}    ");

            _screen.MoveAddString (24, 82, $"{counter.ToString()}    ");
            _screen.Refresh ();
        }
    }
}
