using System.Net;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mindmagma.Curses;
using Modbus.Device;
using System.Linq;
using SoftControllers;
using SampleScenes;
using FTRIG = SoftControllers.FallingEdgeDetector; 
using RTRIG = SoftControllers.RaisingEdgeDetector;
using TON = SoftControllers.OnDelayTimer; 
using CsvHelper;
using System.Dynamic;
using System.Collections.Generic;
namespace SampleScenes.Controllers
{

    /// <summary>
    /// Implements a MODBUS controller for Sorting Station Scene.
    /// <para>
    /// See https://docs.factoryio.com/manual/scenes/sorting-station/
    /// </para>
    /// </summary>
    public class SeparatingStationController : ModbusController 
    {
        Screen _screen;
        Coil entryConveyor1;
        Coil entryConveyor2;
        Coil pusher1;
        Coil pusher2;
        Coil conveyor1;
        Coil conveyor2;

        InputRegister visionSensor1;
        InputRegister visionSensor2;
        DiscreteInput sensor1;
        DiscreteInput sensor2;
        DiscreteInput atPusher1Exit;
        DiscreteInput atPusher2Exit;
        DiscreteInput pusherBack1;
        DiscreteInput pusherFront1;
        DiscreteInput pusherBack2;
        DiscreteInput pusherFront2;

        FTRIG ftSensor1 = new FTRIG();
        FTRIG ftSensor2 = new FTRIG();
        FTRIG ftAtPusher1Exit = new FTRIG();
        FTRIG ftAtPusher2Exit = new FTRIG();

        State state = State.State0;

        int counter;

        public SeparatingStationController (ConnectionInformation connection, RegisterMap registerMap, Screen screen) 
        : base(connection.endPoint, connection.deviceId, new StreamWriter($"Log_{nameof(SeparatingStationController)}.csv")) {
            this._screen = screen;
            this.CycleTime = 50;

             entryConveyor1 = registerMap.GetCoil(Device, "Entry conveyor 1");
             entryConveyor2 = registerMap.GetCoil(Device, "Entry conveyor 2");
             pusher1 = registerMap.GetCoil(Device, "Pusher 1");
             pusher2 = registerMap.GetCoil(Device, "Pusher 2");
             conveyor1 = registerMap.GetCoil(Device, "Conveyor 1");
             conveyor2 = registerMap.GetCoil(Device, "Conveyor 2");

             visionSensor1 = registerMap.GetInputRegister(Device, "Vision sensor 1");
             visionSensor2 = registerMap.GetInputRegister(Device, "Vision sensor 2");
             sensor1 = registerMap.GetDiscreteInput(Device, "Sensor 1");
             sensor2 = registerMap.GetDiscreteInput(Device, "Sensor 2");
             atPusher1Exit = registerMap.GetDiscreteInput(Device, "At pusher 1 exit");
             atPusher2Exit = registerMap.GetDiscreteInput(Device, "At pusher 2 exit");
             pusherBack1 = registerMap.GetDiscreteInput(Device, "Pusher 1 back");
             pusherFront1 = registerMap.GetDiscreteInput(Device, "Pusher 1 front");
             pusherBack2 = registerMap.GetDiscreteInput(Device, "Pusher 2 back");
             pusherFront2 = registerMap.GetDiscreteInput(Device, "Pusher 2 front");

            AutoRegisterDatapoints(this);
        }

        /// <summary>
        /// Performs necessary initialization steps of the factory.
        /// It sets the coils and let the run system dry for 10s.
        /// </summary>
        protected override Task InitializeAsync (CancellationToken token) {

            _screen.MoveAddString (5, 2, $"=== CONTROLLER ===");
            _screen.MoveAddString (6, 2, $"Sorting Station Controller, connected to {Device.EndPoint}/{Device.DeviceId}.");
            _screen.Refresh ();

            entryConveyor1.Value = false;
            entryConveyor2.Value = false;
            pusher1.Value = false;
            pusher2.Value = false;
            conveyor1.Value = false;
            conveyor2.Value = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes single control actions. 
        /// <para>
        /// This code was adapted from: 
        /// https://github.com/realgamessoftware/factoryio-sdk/blob/master/samples/Controllers/Scenes/SortingStation.cs
        /// </para>
        /// </summary>
        protected override Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel)
        {
            ftSensor1.CLK(sensor1.Value);
            ftSensor2.CLK(sensor2.Value);
            ftAtPusher1Exit.CLK(atPusher1Exit.Value);
            ftAtPusher2Exit.CLK(atPusher2Exit.Value);

            if (state == State.State0)
            {
                entryConveyor1.Value = (visionSensor1.Value == 0);
                entryConveyor2.Value = (visionSensor2.Value == 0);
                conveyor1.Value = false;
                conveyor2.Value = false;

                if (visionSensor1.Value != 0 && visionSensor2.Value != 0)
                {
                    if (visionSensor1.Value == 1 && visionSensor2.Value == 4)
                    {
                        entryConveyor1.Value = true;
                        entryConveyor2.Value = true;
                        conveyor1.Value = true;
                        conveyor2.Value = true;

                        counter = 0;

                        state = State.State1;
                    }
                    else
                    {
                        state = State.State10;
                    }
                }
            }
            else if (state == State.State1)
            {
                if (ftSensor1.Q)
                    entryConveyor1.Value = false;

                if (ftSensor2.Q)
                    entryConveyor2.Value = false;

                if (ftAtPusher1Exit.Q)
                {
                    conveyor1.Value = false;
                    counter++;
                }

                if (ftAtPusher2Exit.Q)
                {
                    conveyor2.Value = false;
                    counter++;
                }

                if (counter > 1)
                    state = State.State0;
            }
            else if (state == State.State10)
            {
                if (visionSensor1.Value == 4) //Green
                {
                    entryConveyor1.Value = true;
                    conveyor1.Value = true;

                    state = State.State11;
                }
                else if (visionSensor2.Value == 1) //Blue
                {
                    entryConveyor2.Value = true;
                    conveyor2.Value = true;

                    state = State.State20;
                }
            }
            else if (state == State.State11)
            {
                if (ftSensor1.Q)
                {
                    entryConveyor1.Value = false;
                    conveyor1.Value = false;
                    pusher1.Value = true;

                    state = State.State12;
                }
            }
            else if (state == State.State12)
            {
                if (pusherFront1.Value)
                {
                    pusher1.Value = false;
                    conveyor2.Value = true;
                }

                if (ftAtPusher2Exit.Q)
                    state = State.State0;
            }
            else if (state == State.State20)
            {
                if (ftSensor2.Q)
                {
                    entryConveyor2.Value = false;
                    conveyor2.Value = false;
                    pusher2.Value = true;

                    state = State.State21;
                }
            }
            else if (state == State.State21)
            {
                if (pusherFront2.Value)
                {
                    pusher2.Value = false;
                    conveyor1.Value = true;
                }

                if (ftAtPusher1Exit.Q)
                    state = State.State0;
            }
            _ = Task.Run(() => UpdateUI(elapsedMilliseconds));
            return Task.CompletedTask;
        }
        ElapsedTimeObserver _elapsedObserver = new ElapsedTimeObserver(50);
        private bool updating = false;

        private void UpdateUI (int elapsedMilliseconds) 
        {
            if (updating == true) return;
            _elapsedObserver.Add(elapsedMilliseconds);
            _screen.MoveAddString (7, 2, $"Running time = {_elapsedObserver.RunningTime}");
            _screen.MoveAddString (8, 2, $"Cycle time: avg = {_elapsedObserver.MovingAvg}, max = {_elapsedObserver.Max}, min = {_elapsedObserver.Min}");       

            _screen.MoveAddString (10, 2, $"== STATES ==");
            _screen.MoveAddString (11, 2, $"State = {state.ToString()}   ");
            _screen.MoveAddString (12, 2, $"Counter = {counter.ToString()}   ");


            _screen.MoveAddString (10, 32, $"== SENSORS ==");
            _screen.MoveAddString (11, 32, $"{visionSensor1.ToString()}    ");
            _screen.MoveAddString (12, 32, $"{sensor1.ToString()}    ");
            _screen.MoveAddString (13, 32, $"{atPusher1Exit.ToString()}    ");
            _screen.MoveAddString (14, 32, $"{pusherBack1.ToString()}    ");
            _screen.MoveAddString (15, 32, $"{pusherFront1.ToString()}    ");

            _screen.MoveAddString (17, 32, $"{visionSensor2.ToString()}    ");
            _screen.MoveAddString (18, 32, $"{sensor2.ToString()}    ");
            _screen.MoveAddString (19, 32, $"{atPusher2Exit.ToString()}    ");
            _screen.MoveAddString (20, 32, $"{pusherBack2.ToString()}    ");
            _screen.MoveAddString (12, 32, $"{pusherFront2.ToString()}    ");

            _screen.MoveAddString (10, 82, $"== ACTUATORS ==");
            _screen.MoveAddString (11, 82, $"{entryConveyor1.ToString()}    ");
            _screen.MoveAddString (12, 82, $"{pusher1.ToString()}    ");
            _screen.MoveAddString (13, 82, $"{conveyor1.ToString()}    ");

            _screen.MoveAddString (15, 82, $"{entryConveyor2.ToString()}    ");
            _screen.MoveAddString (16, 82, $"{pusher2.ToString()}    ");
            _screen.MoveAddString (17, 82, $"{conveyor2.ToString()}    ");
 
            _screen.Refresh ();
            updating = false;
        }

        protected override Task FinalizeAsync()
        {
            return Task.CompletedTask;
        }
    }

}
